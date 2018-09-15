using System.Collections.Generic;
using System.IO;

namespace SvcGuest
{
    public static class Globals
    {
        public static Dictionary<string, Dictionary<string, List<string>>> Config;
        public static string ConfigPath { get; set; }

        public static string ServiceName => Path.GetFileNameWithoutExtension(ConfigPath);

        public static string DisplayName
        {
            get
            {
                var ret = Config["Unit"]?["Name"]?[0] ?? Path.GetFileNameWithoutExtension(ConfigPath);
                if (string.IsNullOrEmpty(ret)) ret = "SvcGuest Default Service";
                return ret;
            }
        }

        public static string Description
        {
            get
            {
                var ret = "";
                if (Config["Unit"].ContainsKey("Description")) ret = ret + string.Join("\n", Config["Unit"]["Description"]);
                if (Config["Unit"].ContainsKey("Documentation")) ret = ret + "\n\n" + string.Join("\n", Config["Unit"]["Documentation"]);
                return ret;
            }
        }

        public static string ServiceArguments { get; set; } = "--service";
    }
}
