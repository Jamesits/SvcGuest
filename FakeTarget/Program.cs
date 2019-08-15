using System;
using System.IO;
using System.Linq;

namespace FakeTarget
{
    internal class FuncDef
    {
        public delegate int FunctionType(string[] args);
        public FuncDef(string command, FunctionType function, string help = null, bool hideFromHelp = false)
        {
            Command = command;
            Function = function;
            Help = help;
            HideFromHelp = hideFromHelp;
        }

        public string Command { get; }
        public FunctionType Function { get; }
        public string Help { get; }
        public bool HideFromHelp { get; }

    }

    internal static class Program
    {

        internal static readonly FuncDef[] Functions =
        {
            new FuncDef("help", Help, "Print this message"), 
            new FuncDef("echo", BasicFunctionDef.Echo, "Echo the following text"),
            new FuncDef("delay", BasicFunctionDef.Delay, "Sleep N milliseconds"),
            // ReSharper disable once StringLiteralTypo
            new FuncDef("lorem", BasicFunctionDef.Lorem, "Echo n chars of fake text"), 
            new FuncDef("quit", BasicFunctionDef.Quit, "Quit program with optional ExitCode"),
            new FuncDef("getUser", BasicFunctionDef.GetCurrentUsername, "Echo the username of current security context"), 
            new FuncDef("setOutput", SetOutput, "Set the output mode to stdout|stderr|filename"), 
            new FuncDef("getEnv", BasicFunctionDef.GetEnvironmentVariables, "Echo a list of system environment variables in key=value format"), 
            new FuncDef("getAccessToken", BasicFunctionDef.GetAccessToken, "Get an integer representation of the primary access token of current user"), 
        };

        internal static int Help(string[] args)
        {
            Program.WriteLine("A test program that does harmless pre-defined things for debug use.\n\nUsage: FakeTarget.exe [Options]\n\nOptions:");
            foreach (var func in Functions.OrderBy(funcDef => funcDef.Command))
            {
                if (!func.HideFromHelp)
                    Program.WriteLine($"  -{func.Command,-16} {func.Help}");
            }
            Program.WriteLine();
            return 0;
        }

        internal static int SetOutput(string[] args)
        {
            if (string.Equals(args[1], "stdout", StringComparison.OrdinalIgnoreCase)) CurrentOutputMode = 0;
            else if (string.Equals(args[1], "stderr", StringComparison.OrdinalIgnoreCase)) CurrentOutputMode = 1;
            else
            {
                CurrentOutputStreamWriter = new StreamWriter(string.Join(" ", args.Skip(1).ToArray()));
                CurrentOutputMode = 2;
            }

            if (CurrentOutputMode != 2) CloseOutputStream();

            return 0;
        }

        // 0: stdout
        // 1: stderr
        // 2: file
        internal static int CurrentOutputMode = 0;
        internal static System.IO.StreamWriter CurrentOutputStreamWriter;

        internal static void Write(string s = "")
        {
            if (CurrentOutputMode == 0) Console.Write(s);
            else if (CurrentOutputMode == 1) Console.Error.Write(s);
            else CurrentOutputStreamWriter.Write(s);
        }

        internal static void WriteLine(string s = "") => Write(s + "\n");

        internal static void CloseOutputStream()
        {
            if (CurrentOutputStreamWriter != null)
            {
                CurrentOutputStreamWriter.Flush();
                CurrentOutputStreamWriter.Close();
                CurrentOutputStreamWriter = null;
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            // execute
            var commandStartIndexList = args.Select((value, index) => new { value, index }).Where(item => item.value.StartsWith("-")).Select(item => item.index).ToList();

            for (var i = 0; i < commandStartIndexList.Count; ++i)
            {
                string[] currentArgs;
                try
                {
                    currentArgs = args.Skip(commandStartIndexList[i])
                        .Take(commandStartIndexList[i + 1] - commandStartIndexList[i]).ToArray();
                    
                }
                catch (ArgumentOutOfRangeException)
                {
                    currentArgs = args.Skip(commandStartIndexList[i]).Take(args.Length - commandStartIndexList[i]).ToArray();
                }

                var currentCommand = currentArgs[0].TrimStart('-');
                var currentFuncDef = Functions.Where(def =>
                        string.Equals(def.Command, currentCommand, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (currentFuncDef.Length > 0)
                {
                    var ret = currentFuncDef[0].Function(currentArgs);
                    if (ret != 0) throw new InvalidOperationException($"Failed to execute command {string.Join(" ", currentArgs)}");
                }
                else
                {
                    // command not found
                    throw new NotSupportedException($"Unknown command: {currentCommand}");
                }
            }

            CloseOutputStream();

            return 0;
        }

        /// <summary>
        /// The default global exception handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            var e = eventArgs.ExceptionObject as Exception;
            Program.WriteLine(e?.InnerException.ToString());

            CloseOutputStream();
        }
    }
}
