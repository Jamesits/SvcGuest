using System;
using System.Diagnostics;
using SvcGuest.Logging;

namespace SvcGuest.ProgramWrappers
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
            _p.StartInfo.RedirectStandardError = true;
            _p.StartInfo.WorkingDirectory = Globals.Config.WorkingDirectory;
            _p.StartInfo.LoadUserProfile = true;

            _p.OutputDataReceived += (sender, args) => LogMuxer.Instance.SubprocessStdout(args.Data);
            _p.ErrorDataReceived += (sender, args) => LogMuxer.Instance.SubprocessStderr(args.Data);
            _p.EnableRaisingEvents = true;
            _p.Exited += OnExit;
        }

        public override void Start()
        {
            _p.Start();

            try
            {
                _p.BeginOutputReadLine();
            }
            catch (InvalidOperationException)
            {
                LogMuxer.Instance.Warning("Unable to read stdout");
            }

            try
            {
                _p.BeginErrorReadLine();
            }
            catch (InvalidOperationException)
            {
                LogMuxer.Instance.Warning("Unable to read stderr");
            }
        }

        public override void Stop()
        {
            if (HasExited) return;
            QuitProcess(_p);
        }

        private void OnExit(object sender, EventArgs args)
        {
            OnProgramExited(sender, args);
        }

        public override void WaitForExit()
        {
            _p.WaitForExit();
        }
    }
}
