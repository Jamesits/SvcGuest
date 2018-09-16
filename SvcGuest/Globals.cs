﻿using System.IO;

namespace SvcGuest
{
    public static class Globals
    {
        public static Config Config;
        public static string ConfigPath { get; set; }

        public static string ServiceName => Path.GetFileNameWithoutExtension(ConfigPath);

        public static string ServiceArguments { get; set; } = "--service";
    }
}
