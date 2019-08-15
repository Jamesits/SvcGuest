using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudoTest
{
    public class Util
    {
        public static int RunAndWaitForOutput(string program, string programArgs, out List<string> stdout, out List<string> stderr)
        {
            var _stdout = new List<string>();
            var _stderr = new List<string>();

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = program,
                    Arguments = programArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true,
            };
            p.OutputDataReceived += (sender, args) => _stdout.Add(args.Data);
            p.ErrorDataReceived += (sender, args) => _stderr.Add(args.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            stdout = _stdout;
            stderr = _stderr;
            return p.ExitCode;
        }
    }
}
