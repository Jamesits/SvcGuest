using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SvcGuest.Logging;
using SvcGuest.ProgramWrappers;
using SvcGuest.Win32;

namespace SvcGuest.ServiceInterface
{
    /// <summary>
    /// Supervisor has a simple task: run all the pre-defined things and if they quit, determine
    /// if it is a successful execution.
    /// </summary>
    class Supervisor
    {

        private readonly List<ProgramWrapper> _programPool = new List<ProgramWrapper>();
        private bool hasStartedSuccessfully;
        private bool hasStopped;

        public delegate void SupervisorQuitHandler(object sender, EventArgs e);
        public event SupervisorQuitHandler OnQuit;

        private readonly Object ServiceStateLock = new object();

        internal void Start()
        {
            lock(ServiceStateLock)
            {
                // ExecStartPre
                LogMuxer.Instance.Debug("Executing ExecStartPre");
                for (var i = 0; i < Globals.Config.ExecStartPre.Count; ++i)
                {
                    Run(Globals.Config.ExecStartPre[i], "ExecStartPre", i, isAsync: false);
                }

                // ExecStart
                LogMuxer.Instance.Debug("Executing ExecStart");
                for (var i = 0; i < Globals.Config.ExecStart.Count; ++i)
                {
                    Run(Globals.Config.ExecStart[i], "ExecStart", i);
                }

                // ExecStartPost
                LogMuxer.Instance.Debug("Executing ExecStartPost");
                for (var i = 0; i < Globals.Config.ExecStartPost.Count; ++i)
                {
                    Run(Globals.Config.ExecStartPost[i], "ExecStartPost", i, isAsync: false);
                }

                hasStopped = false;
            }
        }

        internal void Stop()
        {
            lock(ServiceStateLock)
            {
                if (hasStopped) return;
                LogMuxer.Instance.Debug("Killing child processes");
                foreach (var wrapper in _programPool)
                {
                    wrapper.Stop();
                }

                _programPool.Clear();

                // First we notify all running process to quit
                LogMuxer.Instance.Debug("Killing subprocesses");
                foreach (var pid in ProgramWrapper.GetChildProcessIds(ProgramWrapper.SelfProcessId))
                {
                    ProgramWrapper.QuitProcess(Process.GetProcessById(pid));
                }

                // ExecStop
                // only if the service started successfully
                if (hasStartedSuccessfully)
                {
                    LogMuxer.Instance.Debug("Executing ExecStop");
                    for (var i = 0; i < Globals.Config.ExecStop.Count; ++i)
                    {
                        Run(Globals.Config.ExecStop[i], "ExecStop", i, isAsync: false);
                    }
                }

                // ExecStopPost
                LogMuxer.Instance.Debug("Executing ExecStopPost");
                for (var i = 0; i < Globals.Config.ExecStopPost.Count; ++i)
                {
                    Run(Globals.Config.ExecStopPost[i], "ExecStopPost", i, isAsync: false);
                }

                OnQuit?.Invoke(this, null);

                hasStopped = true;
            }
        }

        internal void WaitForExit()
        {
            bool allExited = false;
            while (!hasStopped && !allExited)
            {
                allExited = true;
                foreach (var p in _programPool)
                {
                    if (!p.HasExited)
                    {
                        allExited = false;
                        break;
                    }
                }

                Thread.Sleep(1000);
            }
            
        }

        private void SubprocessQuit(object sender, EventArgs e)
        {
            var p = sender as Process;
            if (p?.ExitCode == 0) hasStartedSuccessfully = true;
            if (!Globals.Config.RemainAfterExit && _programPool.All(x => x.HasExited)) Stop();
        }

        private void Run(ExecConfig e, string section, int seq, bool isAsync = true)
        {
            ProgramWrapper wrapper;
            if (Globals.Config.User == null
                || e.ExecLaunchPrivilegeLevel == ExecLaunchPrivilegeLevel.IgnoreUser
                || e.ExecLaunchPrivilegeLevel == ExecLaunchPrivilegeLevel.Full)
            {
                wrapper = new ManagedProgramWrapper(e.ProgramPath, e.Arguments);
                wrapper.Start();
            }
            else
            {
                // Get the privilege for impersonation
                var currentProcess = new CProcess();
                if (!currentProcess.SetPrivilege("SeTcbPrivilege", true))
                {
                    throw new InvalidOperationException("Required privilege SeTcbPrivilege failed");
                }
                if (!currentProcess.SetPrivilege("SeDelegateSessionUserImpersonatePrivilege", true))
                {
                    throw new InvalidOperationException("Required privilege SeDelegateSessionUserImpersonatePrivilege failed");
                }

                // Get the identity we needed
                var identity = new WindowsIdentity(Globals.Config.User);
                if (identity.ImpersonationLevel != TokenImpersonationLevel.Impersonation)
                {
                    throw new InvalidOperationException("Insufficient permission");
                }

                // Run the helper process as that identity
                using (identity.Impersonate())
                {
                    LogMuxer.Instance.Debug($"After impersonation, User={WindowsIdentity.GetCurrent().Name}, ImpersonationLevel={identity.ImpersonationLevel}");;

                    wrapper = new NativeProgramWrapper(section, seq, identity.Token);
                    wrapper.Start();
                }
            }

            if (isAsync)
            {
                _programPool.Add(wrapper);
                wrapper.ProgramExited += SubprocessQuit;
            }
            else
            {
                wrapper.WaitForExit();
            }
        }
    }
}
