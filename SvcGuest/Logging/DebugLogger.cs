namespace SvcGuest.Logging
{
    class DebugLogger : Logger
    {
        internal override void Debug(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void Info(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void Warning(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void Error(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void Fatal(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void SuccessAudit(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void FailureAudit(string s) => System.Diagnostics.Debug.WriteLine(s);
        internal override void SubprocessStdout(string s)
        {
            if (s != null) System.Diagnostics.Debug.WriteLine(s);
        }

        internal override void SubprocessStderr(string s)
        {
            if (s != null) System.Diagnostics.Debug.WriteLine(s);
        }

        internal override void Flush(){}
    }
}
