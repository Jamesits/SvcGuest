using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using SvcGuest.ProgramWrappers;
using SvcGuest.Win32;

namespace SvcGuest.ServiceInterface
{
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
        private readonly List<ProgramWrapper> _execStartProgramPool = new List<ProgramWrapper>();

        public SupervisorService()
        {
            ServiceName = Globals.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_START_PENDING;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);

            if (Globals.Config.Type == ServiceType.Simple)
            {
                // expect there is only one ExecStart=
                var mainProgram = Globals.Config.ExecStart[0];
                if (Globals.Config.User == null)
                {
                    var wrapper = new ManagedProgramWrapper(mainProgram.ProgramPath, mainProgram.Arguments);
                    wrapper.ProgramExited += (sender, eventArgs) => StopOnError();
                    _execStartProgramPool.Add(wrapper);
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
                    using (WindowsImpersonationContext impersonatedUser = identity?.Impersonate())
                    {
                        Debug.WriteLine("After impersonation: " + WindowsIdentity.GetCurrent().Name);
                        Debug.WriteLine(identity.ImpersonationLevel);

                        var wrapper = new NativeProgramWrapper("ExecStart", 0, identity.Token);
                        wrapper.ProgramExited += (sender, eventArgs) => StopOnError();
                        _execStartProgramPool.Add(wrapper);
                        wrapper.Start();
                    }
                }
                
            }

            base.OnStart(args);

            // Update the service state to Running.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_RUNNING;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);
        }

        private void StopOnError()
        {
            base.Stop();
        }

        protected override void OnStop()
        {
            Debug.WriteLine("Stopping service");
            // Update the service state to Stop Pending.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOP_PENDING;
            _serviceStatus.dwWaitHint = ProgramWrapper.KillWaitMs;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);

            foreach (var wrapper in _execStartProgramPool)
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