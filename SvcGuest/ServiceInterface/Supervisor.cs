using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
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

        public delegate void SupervisorQuitHandler(object sender, EventArgs e);
        public event SupervisorQuitHandler OnQuit;

        internal void Start()
        {
            // ExecStartPre
            Debug.WriteLine("Executing ExecStartPre");
            for (var i = 0; i < Globals.Config.ExecStartPre.Count; ++i)
            {
                Run(Globals.Config.ExecStartPre[i], "ExecStartPre", i, isAsync: false);
            }

            // ExecStart
            Debug.WriteLine("Executing ExecStart");
            for (var i = 0; i < Globals.Config.ExecStart.Count; ++i)
            {
                Run(Globals.Config.ExecStart[i], "ExecStart", i);
            }

            // ExecStartPost
            Debug.WriteLine("Executing ExecStartPost");
            for (var i = 0; i < Globals.Config.ExecStartPost.Count; ++i)
            {
                Run(Globals.Config.ExecStartPost[i], "ExecStartPost", i, isAsync: false);
            }

        }

        internal void Stop()
        {
            Debug.WriteLine("Killing child processes");
            foreach (var wrapper in _programPool)
            {
                wrapper.Stop();
            }

            // First we notify all running process to quit
            Debug.WriteLine("Killing subprocesses");
            foreach (var pid in ProgramWrapper.GetChildProcessIds(ProgramWrapper.SelfProcessId))
            {
                ProgramWrapper.QuitProcess(Process.GetProcessById(pid));
            }

            // ExecStop
            // only if the service started successfully
            if (hasStartedSuccessfully)
            {
                Debug.WriteLine("Executing ExecStop");
                for (var i = 0; i < Globals.Config.ExecStop.Count; ++i)
                {
                    Run(Globals.Config.ExecStop[i], "ExecStop", i, isAsync: false);
                }
            }
            
            // ExecStopPost
            Debug.WriteLine("Executing ExecStopPost");
            for (var i = 0; i < Globals.Config.ExecStopPost.Count; ++i)
            {
                Run(Globals.Config.ExecStopPost[i], "ExecStopPost", i, isAsync: false);
            }

            OnQuit?.Invoke(this, null);

        }

        internal void WaitForExit()
        {
            foreach (var p in _programPool)
                p.WaitForExit();
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
                    Debug.WriteLine("After impersonation: " + WindowsIdentity.GetCurrent().Name);
                    Debug.WriteLine(identity.ImpersonationLevel);

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
