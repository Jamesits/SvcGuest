using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

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

        [DllImport("advapi32.dll", SetLastError = true)]
        // use this.ServiceHandle
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

            // TODO: correct parsing of default value
            if (Globals.Config["Service"]["Type"][0] == "simple")
            {
                var prog = Globals.Config["Service"]["ExecStart"][0].Trim();
                string cmd, cargs;
                SplitCommandline(prog, out cmd, out cargs);
                Debug.WriteLine(cmd);
                Debug.WriteLine(cargs);
                var wrapper = new ProgramWrapper(cmd, cargs);
                _execStartProgramPool.Add(wrapper);
                wrapper.Start();
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

        internal void SplitCommandline(string cmd, out string program, out string args)
        {
            var i = 0;
            var endToken = ' ';
            cmd = cmd.Trim();
            for (; i < cmd.Length; ++i)
            {
                if (endToken == ' ' && cmd[i] == endToken) break;
                if (cmd[i] == endToken) endToken = ' ';
                if (cmd[i] == '\'' || cmd[i] == '\"') endToken = cmd[i];
            }

            program = cmd.Substring(0, i);
            args = cmd.Substring(i, cmd.Length - i);
        }
    }
}