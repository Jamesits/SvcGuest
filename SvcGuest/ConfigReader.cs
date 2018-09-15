using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SvcGuest
{
    public class ConfigReader
    {
        public readonly Dictionary<string, Dictionary<string, List<string>>> Config = new Dictionary<string, Dictionary<string, List<string>>>();

        public ConfigReader(string filename)
        {
            var currentSection = "";
            string line;
            uint lineNum = 0;
            var file = new StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                lineNum++;
                Console.WriteLine(line);
                line = line.Trim();

                // comments
                if (line.Length == 0 || line.StartsWith("#")) continue;

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

                if (!Config.ContainsKey(currentSection))
                {
                    Config[currentSection] = new Dictionary<string, List<string>>();
                }

                try
                {
                    var kvList = line.Split("=".ToCharArray(), 2);
                    kvList[0] = kvList[0].Trim();
                    kvList[1] = kvList[1].Trim();

                    if (!Config[currentSection].ContainsKey(kvList[0]))
                    {
                        Config[currentSection][kvList[0]] = new List<string>();
                    }

                    if (kvList[1] == "")
                    {
                        Config[currentSection][kvList[0]] = new List<string>();
                    } else {
                        Config[currentSection][kvList[0]].Add(kvList[1]);
                    }

                }
                catch
                {
                    Console.WriteLine($"Unable to parse line #{lineNum}: {line}");
                }

            }
            file.Close();
        }
    }
}
