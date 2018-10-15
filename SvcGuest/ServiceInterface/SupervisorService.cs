using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using SvcGuest.ProgramWrappers;
using SvcGuest.Win32;

namespace SvcGuest.ServiceInterface
{
    /// <inheritdoc />
    /// <summary>
    /// The service itself. Will be invoked by system.
    /// </summary>
    public class SupervisorService : ServiceBase
    {
        private DeepDarkWin32Fantasy.ServiceStatus _serviceStatus = new DeepDarkWin32Fantasy.ServiceStatus()
        {
            dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOPPED,
            dwWaitHint = 1000,

        };
        private readonly List<ProgramWrapper> _programPool = new List<ProgramWrapper>();
        private bool _isErrorQuitting;

        public SupervisorService()
        {
            ServiceName = Globals.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.  
            Debug.WriteLine("Setting service status to start pending");
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_START_PENDING;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);

            base.OnStart(args);

            // ExecStartPre
            for (var i = 0; i < Globals.Config.ExecStartPre.Count; ++i)
            {
                Run(Globals.Config.ExecStartPre[i], "ExecStartPre", i);
            }


            // ExecStart
            for (var i = 0; i < Globals.Config.ExecStart.Count; ++i)
            {
                Run(Globals.Config.ExecStart[i], "ExecStart", i);
            }

            // ExecStartPost
            for (var i = 0; i < Globals.Config.ExecStartPost.Count; ++i)
            {
                Run(Globals.Config.ExecStartPost[i], "ExecStartPost", i);
            }

            // Update the service state to Running.  
            Debug.WriteLine("Setting service status to running");
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_RUNNING;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);
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
                wrapper.ProgramExited += (sender, eventArgs) => StopOnError();
            }
            else
            {
                wrapper.WaitForExit();
            }
        }

        private void StopOnError()
        {
            _isErrorQuitting = true;
            if (!Globals.Config.RemainAfterExit) Stop();
        }

        protected override void OnStop()
        {
            Debug.WriteLine("Stopping service");
            // Update the service state to Stop Pending.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOP_PENDING;
            _serviceStatus.dwWaitHint = ProgramWrapper.KillWaitMs;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);

            // ExecStop
            if (_isErrorQuitting)
                for (var i = 0; i < Globals.Config.ExecStop.Count; ++i)
                {
                    Run(Globals.Config.ExecStop[i], "ExecStop", i, isAsync: false);
                }

            // ExecStopPost
            for (var i = 0; i < Globals.Config.ExecStopPost.Count; ++i)
            {
                Run(Globals.Config.ExecStopPost[i], "ExecStopPost", i);
            }

            foreach (var wrapper in _programPool)
            {
                wrapper.Stop();
            }

            foreach (var pid in ProgramWrapper.GetChildProcessIds(ProgramWrapper.SelfProcessId))
            {
                ProgramWrapper.QuitProcess(Process.GetProcessById(pid));
            }

            base.OnStop();

            // Update the service state to Stopped.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOPPED;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);
        }
    }
}