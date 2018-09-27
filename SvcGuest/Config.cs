using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SvcGuest
{
    public enum ServiceType {
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Global
        Simple,
        Forking,
        Oneshot,
        Dbus,
        Notify,
        Idle,
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Global
    }

    public enum ExecLaunchPrivilegeLevel
    {
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Global
        Full, // "+"
        IgnoreUser, // "!"
        Normal, // ordinary case
        Least, // "!!"
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Global
    }

    public class ExecConfig
    {
        public bool IgnoreFirstArg { get; set; }
        public bool IgnoreFailure { get; set; }
        public ExecLaunchPrivilegeLevel ExecLaunchPrivilegeLevel { get; set; } = ExecLaunchPrivilegeLevel.Normal;
        public string ProgramPath { get; set; }
        public string Arguments { get; set; }

        public ExecConfig(string cmd)
        {
            var specialChars = @"@-+!".ToCharArray();
            int cmdScanIndex;

            cmd = cmd.Trim();

            for (cmdScanIndex = 0; specialChars.Contains(cmd[cmdScanIndex]); ++cmdScanIndex)
            {
                switch (cmd[cmdScanIndex])
                {
                    case '@':
                        IgnoreFirstArg = true;
                        break;
                    case '-':
                        IgnoreFailure = true;
                        break;
                    case '+':
                        ExecLaunchPrivilegeLevel = ExecLaunchPrivilegeLevel.Full;
                        break;
                    case '!':
                        switch (ExecLaunchPrivilegeLevel)
                        {
                            case ExecLaunchPrivilegeLevel.Normal:
                                ExecLaunchPrivilegeLevel = ExecLaunchPrivilegeLevel.IgnoreUser;
                                break;
                            case ExecLaunchPrivilegeLevel.IgnoreUser:
                                ExecLaunchPrivilegeLevel = ExecLaunchPrivilegeLevel.Least;
                                break;
                            default:
                                throw new ArgumentException();
                        }
                        break;
                }
            }


            var insideQuote = false;
            for (; cmdScanIndex < cmd.Length; ++cmdScanIndex)
            {
                switch (cmd[cmdScanIndex])
                {
                    case ' ':
                        if (!insideQuote) goto Found;
                        break;
                    case '"':
                        if (insideQuote)
                        {
                            ++cmdScanIndex;
                            goto Found;
                        }
                        insideQuote = true;
                        break;
                }
            }

            Found:
            ProgramPath = cmd.Substring(0, cmdScanIndex);
            Arguments = cmd.Substring(cmdScanIndex, cmd.Length - cmdScanIndex);
        }
    }

    /// <summary>
    /// A hand written ini-like file parser.
    /// </summary>
    public class Config
    {
        public readonly Dictionary<string, Dictionary<string, List<string>>> RawConfig = new Dictionary<string, Dictionary<string, List<string>>>();
        public string FirstConfigPath;

        // [Unit]
        public string Name => GetValue("Unit", "Name") ?? Path.GetFileNameWithoutExtension(FirstConfigPath) ?? "SvcGuest Default Service";
        public string Description => GetValue("Unit", "Description");
        public string Documentation => GetValue("Unit", "Documentation");

        // [Service]
        public ServiceType Type
        {
            get
            {
                switch (GetValue("Service", "Type"))
                {
                    case "simple":
                        return ServiceType.Simple;
                    case "forking":
                        return ServiceType.Forking;
                    case "oneshot":
                        return ServiceType.Oneshot;
                    case "dbus":
                        return ServiceType.Dbus;
                    case "notify":
                        return ServiceType.Notify;
                    case "idle":
                        return ServiceType.Idle;
                    default:
                        return ServiceType.Simple;
                }
            }
        }

        public List<ExecConfig> ExecStartPre => GetExecConfigs("Service", "ExecStartPre");
        public List<ExecConfig> ExecStart => GetExecConfigs("Service", "ExecStart");
        public List<ExecConfig> ExecStartPost => GetExecConfigs("Service", "ExecStartPost");
        public List<ExecConfig> ExecStop => GetExecConfigs("Service", "ExecStop");
        public List<ExecConfig> ExecStopPost => GetExecConfigs("Service", "ExecStopPost");

        public string WorkingDirectory => GetValue("Service", "WorkingDirectory");
        public string User => GetValue("Service", "User");
        public bool RemainAfterExit => GetValue("Service", "RemainAfterExit", false);

        // ======================= Config parser ============================

        //public override string ToString()
        //{
        //    StringBuilder sb = new StringBuilder("Current config:");
        //    return sb.ToString();
        //}

        public Config() { }

        public Config(string filename): this()
        {
            AppendFile(filename);
        }

        public void AppendFile(string filename)
        {
            if (FirstConfigPath == null) FirstConfigPath = filename;

            var currentSection = "";
            string line;
            uint lineNum = 0;
            var file = new StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                lineNum++;
                line = line.Trim();

                // comments
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;

                // section header
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    // got a new section
                    currentSection = line.Substring(1, line.Length - 2);
                    continue;
                }

                // values
                if (currentSection == "")
                {
                    Console.WriteLine($"Unable to parse line #{lineNum}: Unexpected value");
                    continue;
                }

                if (!RawConfig.ContainsKey(currentSection))
                {
                    RawConfig[currentSection] = new Dictionary<string, List<string>>();
                }

                try
                {
                    var kvList = line.Split("=".ToCharArray(), 2);
                    kvList[0] = kvList[0].Trim();
                    kvList[1] = kvList[1].Trim();

                    // systemd rule: if value is empty, clear the list; otherwise append to the list
                    if (!RawConfig[currentSection].ContainsKey(kvList[0]))
                    {
                        RawConfig[currentSection][kvList[0]] = new List<string>();
                    }

                    if (kvList[1] == "")
                    {
                        RawConfig[currentSection][kvList[0]] = new List<string>();
                    }
                    else
                    {
                        RawConfig[currentSection][kvList[0]].Add(kvList[1]);
                    }

                }
                catch
                {
                    Console.WriteLine($"Unable to parse line #{lineNum}: {line}");
                }

            }
            file.Close();
        }

        private string GetValue(string section, string key)
        {
            if (!RawConfig.ContainsKey(section) || !RawConfig[section].ContainsKey(key)) return null;
            var len = RawConfig[section][key].Count;
            if (len == 0 || RawConfig[section][key][len - 1].Length == 0) return null;
            return RawConfig[section][key][len - 1];
        }

        private T GetValue<T>(string section, string key)
        {
            return (T)Convert.ChangeType(GetValue(section, key), typeof(T));
        }

        private T GetValue<T>(string section, string key, T defaultValue)
        {
            try
            {
                return (T) Convert.ChangeType(GetValue(section, key), typeof(T));
            }
            catch
            {
                return defaultValue;
            }
                
        }

        // ReSharper disable once UnusedMember.Local
        private List<string> GetValues(string section, string key)
        {
            if (!RawConfig.ContainsKey(section) || !RawConfig[section].ContainsKey(key)) return null;
            var len = RawConfig[section][key].Count;
            if (len == 0) return null;
            return RawConfig[section][key];
        }

        private List<ExecConfig> GetExecConfigs(string section, string key)
        {
            if (!RawConfig.ContainsKey(section) || !RawConfig[section].ContainsKey(key)) return null;
            var len = RawConfig[section][key].Count;
            if (len == 0) return null;
            return RawConfig[section][key].Select(x => new ExecConfig(x)).ToList();
        }
    }
}
