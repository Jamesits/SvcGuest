using System;
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
    class Program
    {

        internal static readonly FuncDef[] Functions =
        {
            new FuncDef("help", Help, "Print this message"), 
        };

        internal static int Help(string[] args)
        {
            Console.WriteLine("A test program that does harmless pre-defined things for debug use.\n\nUsage: FakeTarget.exe [Options]\n\nOptions:");
            foreach (var func in Functions.OrderBy(funcDef => funcDef.Command))
            {
                if (!func.HideFromHelp)
                    Console.WriteLine($"  -{func.Command}\t\t{func.Help}");
            }
            Console.WriteLine();
            return 0;
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
                        string.Equals(def.Command.ToLower(), currentCommand.ToLower(), StringComparison.Ordinal))
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
            Console.WriteLine(e?.InnerException);
        }
    }
}
