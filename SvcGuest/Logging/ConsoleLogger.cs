using System;

namespace SvcGuest.Logging
{
    class ConsoleLogger : Logger
    {
        internal override void Debug(string s) => Console.Out.WriteLine(s);
        internal override void Info(string s) => Console.Out.WriteLine(s);
        internal override void Warning(string s) => Console.Error.WriteLine(s);
        internal override void Error(string s) => Console.Error.WriteLine(s);
        internal override void Fatal(string s) => Console.Error.WriteLine(s);
        internal override void SuccessAudit(string s) => Console.Out.WriteLine(s);
        internal override void FailureAudit(string s) => Console.Error.WriteLine(s);
        internal override void SubprocessStdout(string s)
        {
            if (s != null) Console.Out.WriteLine(s);
        }

        internal override void SubprocessStderr(string s)
        {
            if (s != null) Console.Error.WriteLine(s);
        }

        internal override void Flush()
        {
            Console.Out.Flush();
            Console.Error.Flush();
        }
    }
}
