﻿using System.IO;

namespace SvcGuest
{
    /// <summary>
    /// Global accessible configs, constants, etc.
    /// </summary>
    public static class Globals
    {
        public static Config Config;
        public static string ConfigPath { get; set; }

        public static string ServiceName => Path.GetFileNameWithoutExtension(ConfigPath);


        public static string ServiceArguments { get; set; } = "--service";

        public static string ExecutablePath => System.Reflection.Assembly.GetExecutingAssembly().Location;

        public static string ExecutableDirectory => Path.GetDirectoryName(ExecutablePath);
    }
}
