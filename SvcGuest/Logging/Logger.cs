namespace SvcGuest.Logging
{
    internal abstract class Logger
    {
        internal abstract void Debug(string s);
        internal abstract void Info(string s);
        internal abstract void Warning(string s);
        internal abstract void Error(string s);
        internal abstract void Fatal(string s);
        internal abstract void SuccessAudit(string s);
        internal abstract void FailureAudit(string s);
        internal abstract void SubprocessStdout(string s);
        internal abstract void SubprocessStderr(string s);

        internal abstract void Flush();
    }
}
