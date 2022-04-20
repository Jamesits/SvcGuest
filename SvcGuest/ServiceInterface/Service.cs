using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using SvcGuest.ProgramWrappers;
using LibSudo.Win32;

namespace SvcGuest.ServiceInterface
{
    /// <inheritdoc />
    /// <summary>
    /// The service itself. Will be invoked by system.
    /// </summary>
    public class Service : ServiceBase
    {
        private DeepDarkWin32Fantasy.ServiceStatus _serviceStatus = new DeepDarkWin32Fantasy.ServiceStatus()
        {
            dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOPPED,
            dwWaitHint = 1000,

        };

        private readonly Supervisor supervisor = new Supervisor();
        

        public Service()
        {
            ServiceName = Globals.ServiceName;
            supervisor.OnQuit += StopOnError;
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.  
            Debug.WriteLine("Setting service status to start pending");
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_START_PENDING;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);

            supervisor.Start();
            
            // Update the service state to Running.  
            Debug.WriteLine("Setting service status to running");
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_RUNNING;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);
        }

        private void StopOnError(object sender, EventArgs e)
        {
            // _isErrorQuitting = true;
            if (!Globals.Config.RemainAfterExit) Stop();
        }

        protected override void OnStop()
        {
            Debug.WriteLine("Stopping service");
            // Update the service state to Stop Pending.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOP_PENDING;
            _serviceStatus.dwWaitHint = ProgramWrapper.KillWaitMs;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);

            supervisor.Stop();

            // Update the service state to Stopped.  
            _serviceStatus.dwCurrentState = DeepDarkWin32Fantasy.ServiceState.SERVICE_STOPPED;
            Advapi32.SetServiceStatus(ServiceHandle, ref _serviceStatus);
        }
    }
}