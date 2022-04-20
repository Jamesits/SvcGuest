﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudo
{
    class Program
    {
        static void Main(string[] args)
        {
            var sudo = new LibSudo.Sudo
            {
                CommandLine = @"C:\Windows\system32\cmd.exe /K"
            };
            Debug.WriteLine("sudo object created");

            sudo.Start();
            Debug.WriteLine("program executed");

            var ret = sudo.Wait();
            Debug.WriteLine($"program exited, code: {ret}");
            Environment.ExitCode = (int)ret;
        }
    }
}
