using System;
using System.Diagnostics;
using McMaster.Extensions.CommandLineUtils;

namespace Sudo
{
    [Command(
        UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.StopParsingAndCollect, 
        AllowArgumentSeparator = true,
        ExtendedHelpText = "Switch to another user and set other various security options when launching another program."
        )]
    public class Program
    {
        // command line arguments


        public string[] RemainingArguments { get; } 

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private void OnExecute()
        {
            // argument processing
            var commandLine = string.Join(" ", RemainingArguments);
            Debug.WriteLine($"Executing: {commandLine}");

            // elevation
            LibSudo.Sudo.ElevateSelf();

            // execution
            using (var sudo = new LibSudo.Sudo
                   {
                       CommandLine = commandLine,
                       NewConsole = false,
                   })
            {
                Debug.WriteLine("sudo object created");

                sudo.Start();
                Debug.WriteLine("program executed");

                var ret = sudo.Wait();
                Debug.WriteLine($"program exited, code: {ret}");
                Environment.ExitCode = (int)ret;
            }
        }
    }
}
