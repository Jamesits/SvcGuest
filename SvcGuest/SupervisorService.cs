using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;

namespace SvcGuest
{
    public class SupervisorService : ServiceBase
    {

        public enum ServiceState
        {
            // ReSharper disable InconsistentNaming
            // ReSharper disable UnusedMember.Global
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
            // ReSharper restore InconsistentNaming
            // ReSharper restore UnusedMember.Global
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        // ReSharper disable once StringLiteralTypo
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        private ServiceStatus _serviceStatus = new ServiceStatus()
        {
            dwCurrentState = ServiceState.SERVICE_STOPPED,
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
            _serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            SetServiceStatus(ServiceHandle, ref _serviceStatus);

            if (Globals.Config.Type == ServiceType.Simple)
            {
                // expect there is only one ExecStart=
                var mainProgram = Globals.Config.ExecStart[0];
                if (Globals.Config.User == null)
                {
                    var wrapper = new ProgramWrapper(mainProgram.ProgramPath, mainProgram.Arguments);
                    wrapper.ProgramExited += (sender, eventArgs) => OnStop();
                    _execStartProgramPool.Add(wrapper);
                    wrapper.Start();
                }
                else
                {
                    // we need to impersonate someone!
                    Debug.WriteLine("Check impersonating privilege");
                    var currentProcess = new CProcess();
                    if (!currentProcess.SetPrivilege("SeTcbPrivilege", true))
                    {
                        throw new InvalidOperationException("Required privilege SeTcbPrivilege failed");
                    }
                    if (!currentProcess.SetPrivilege("SeDelegateSessionUserImpersonatePrivilege", true))
                    {
                        throw new InvalidOperationException("Required privilege SeDelegateSessionUserImpersonatePrivilege failed");
                    }

                    var identity = new WindowsIdentity(Globals.Config.User);
                    if (identity.ImpersonationLevel != TokenImpersonationLevel.Impersonation)
                    {
                        throw new InvalidOperationException("Insufficient permission");
                    }

                    using (WindowsImpersonationContext impersonatedUser = identity?.Impersonate())
                    {
                        Debug.WriteLine("After impersonation: " + WindowsIdentity.GetCurrent().Name);
                        Debug.WriteLine(identity.ImpersonationLevel);

                        // Declare variables
                        Kernel32.PROCESS_INFORMATION pi;
                        Kernel32.STARTUPINFO si = new Kernel32.STARTUPINFO();

                        // Initialize structs
                        si.cb = Marshal.SizeOf(si);

                        Advapi32.SECURITY_ATTRIBUTES saProcessAttributes = new Advapi32.SECURITY_ATTRIBUTES();
                        Advapi32.SECURITY_ATTRIBUTES saThreadAttributes = new Advapi32.SECURITY_ATTRIBUTES();

                        // NOTE: the argument need a space on the front
                        var execArgs = $"\"{Globals.ExecutablePath}\" --impersonated --LaunchType ExecStart --LaunchIndex 0 --config {Globals.ConfigPath}";

                        if (!Advapi32.CreateProcessAsUser(identity.Token, Globals.ExecutablePath, execArgs,
                            ref saProcessAttributes, ref saThreadAttributes, false, 0, IntPtr.Zero, Globals.ExecutableDirectory, ref si, out pi))
                            throw new Win32Exception();
                        Debug.WriteLine($"Helper process at {pi.dwProcessId}");
                    }
                }
                
            }

            base.OnStart(args);

            // Update the service state to Running.  
            _serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(ServiceHandle, ref _serviceStatus);
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.  
            _serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            SetServiceStatus(ServiceHandle, ref _serviceStatus);

            foreach (var wrapper in _execStartProgramPool)
            {
                wrapper.Stop();
            }

            base.OnStop();

            // Update the service state to Stopped.  
            _serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(ServiceHandle, ref _serviceStatus);
        }

        // ReSharper disable once RedundantOverriddenMember
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}