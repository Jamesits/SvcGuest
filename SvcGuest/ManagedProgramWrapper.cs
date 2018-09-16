using System;
using System.Diagnostics;

namespace SvcGuest
{
    /// <summary>
    /// Run external program as self, cannot switch user, but have managed events for program exit.
    /// </summary>
    class ManagedProgramWrapper : ProgramWrapper
    {

        private readonly Process _p = new Process();

        // ReSharper disable once UnusedMember.Global
        public ManagedProgramWrapper(string executableName) : this(executableName, null)
        {

        }

        public ManagedProgramWrapper(string executableName, string arguments)
        {
            // initialize process
            _p.StartInfo.FileName = executableName;
            _p.StartInfo.Arguments = arguments;
            _p.StartInfo.UseShellExecute = false;
            _p.StartInfo.RedirectStandardOutput = true;
            _p.StartInfo.WorkingDirectory = Globals.Config.WorkingDirectory;
            _p.StartInfo.LoadUserProfile = true;

            _p.OutputDataReceived += (sender, args) => OnMessage(args.Data, false);
            _p.ErrorDataReceived += (sender, args) => OnMessage(args.Data, true);
            _p.EnableRaisingEvents = true;
            _p.Exited += OnExit;
        }

        public override void Start()
        {
            _p.Start();

            try
            {
                _p.BeginOutputReadLine();
                _p.BeginErrorReadLine();
            }
            catch (InvalidOperationException)
            {
                EventLog.WriteEntry(EventSourceName, "Unable to read stdout/stderr", EventLogEntryType.FailureAudit);
            }
        }

        public override void Stop()
        {
            QuitProcess(_p);
        }

        private void OnExit(object sender, EventArgs args)
        {
            OnProgramExited(sender, args);
        }
    }
}
