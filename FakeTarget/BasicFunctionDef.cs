using System;
using System.Collections;
using System.Linq;

namespace FakeTarget
{
    internal static class BasicFunctionDef
    {
        internal static int Echo(string[] args)
        {
            Program.WriteLine(string.Join(" ", args.Skip(1).ToArray()));
            return 0;
        }

        internal static int Delay(string[] args)
        {
            int delay = Convert.ToInt32(args[1]);
            System.Threading.Thread.Sleep(delay);
            return 0;
        }

        // ReSharper disable once IdentifierTypo
        internal static int Lorem(string[] args)
        {
            // ReSharper disable StringLiteralTypo
            // ReSharper disable once IdentifierTypo
            // ReSharper disable once IdentifierTypo
            const string loremIpsum =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. ";
            // ReSharper enable StringLiteralTypo
            int chars = Convert.ToInt32(args[1]);
            string output = loremIpsum;
            while (output.Length < chars) output += loremIpsum;
            Program.WriteLine(string.Join("", output.Take(chars).ToArray()));
            return 0;
        }

        internal static int Quit(string[] args)
        {
            if (args.Length < 2) Environment.Exit(0);
            else Environment.Exit(Convert.ToInt32(args[1]));

            Program.CloseOutputStream();

            // to make static checker happy
            return 0;
        }

        internal static int GetCurrentUsername(string[] args)
        {
            Program.WriteLine(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
            return 0;
        }

        internal static int GetEnvironmentVariables(string[] args)
        {
            foreach (DictionaryEntry env in System.Environment.GetEnvironmentVariables())
            {
                Program.WriteLine($"{env.Key}={env.Value}");
            }

            return 0;
        }
    }
}
