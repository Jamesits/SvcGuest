using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using SudoLib.Win32;

namespace SudoLib
{
    public enum IORedirectionMode
    {
        Dispose = 0,
        Stdin,
        Stdout,
        Stderr,
        Pipe,
        File,
    }
    public class SudoConfig
    {
        public string Program { get; set; }

        public bool ExtendProgramPath { get; set; } = true;

        public string Arguments { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ArgumentsCameWithProgramName { get; set; } = false;

        private string _username;

        public string UserName {
            get => _username ?? Environment.UserDomainName;
            set => _username = value;
        }

        private string _workingDirectory;

        public string WorkingDirectory
        {
            get => _workingDirectory ?? System.IO.Directory.GetCurrentDirectory();
            set => _workingDirectory = value;
        }

        public DeepDarkWin32Fantasy.ShowWindowCommands WindowMode { get; set; } =
            DeepDarkWin32Fantasy.ShowWindowCommands.SW_NORMAL;

        // timing

        public int QuitWaitTimeMs { get; set; } = 5000;

        /// <summary>
        /// wait time to check the status of the child process and check if there are new data in pipes
        /// </summary>
        public uint EventWaitTimeMs { get; set; } = 100;
    }
}
