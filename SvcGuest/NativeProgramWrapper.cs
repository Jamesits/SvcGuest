using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;

namespace SvcGuest
{
    /// <summary>
    /// Run external program as another user.
    /// Context switching is handled by native code.
    /// Do not have events, need to poll to check process status.
    /// </summary>
    class NativeProgramWrapper : ProgramWrapper
    {

        private DeepDarkWin32Fantasy.PROCESS_INFORMATION _pi;
        private DeepDarkWin32Fantasy.STARTUPINFO _si;
        private Advapi32.SECURITY_ATTRIBUTES _saProcessAttributes;
        private Advapi32.SECURITY_ATTRIBUTES _saThreadAttributes;

        private readonly string _launchType;
        private readonly int _launchIndex;
        private readonly IntPtr _identityToken;

        public int ChildProcessId => _pi.dwProcessId;

        private readonly Timer _checkChildProcessTimer = new Timer()
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000,
        };

        public NativeProgramWrapper(string launchType, int launchIndex, IntPtr identityToken)
        {
            _launchType = launchType;
            _launchIndex = launchIndex;
            _identityToken = identityToken;

            _si.cb = Marshal.SizeOf(_si);
        }

        public override void Start()
        {
            var execArgs = $"\"{Globals.ExecutablePath}\" --config {Globals.ConfigPath} --impersonated --LaunchType {_launchType} --LaunchIndex {_launchIndex}";

            if (!Advapi32.CreateProcessAsUser(
                _identityToken, 
                Globals.ExecutablePath, 
                execArgs,
                ref _saProcessAttributes, 
                ref _saThreadAttributes, 
                false, 
                0, 
                IntPtr.Zero, 
                Globals.ExecutableDirectory, 
                ref _si, 
                out _pi
                ))
                throw new Win32Exception();
            Debug.WriteLine($"Helper process at {ChildProcessId}");
            _checkChildProcessTimer.Elapsed += OnCheckChildProcessTimer;
        }

        public override void Stop()
        {
            if (!IfChildProcessAlive()) return;
            var cp = Process.GetProcessById(ChildProcessId);
            QuitProcess(cp);
        }

        public bool IfChildProcessAlive()
        {
            return GetChildProcessIds(SelfProcessId).Contains(ChildProcessId);
        }

        private void OnCheckChildProcessTimer(object sender, EventArgs e)
        {
            if (!IfChildProcessAlive())
            {
                OnProgramExited(this, null);
                _checkChildProcessTimer.Enabled = false;
            }
        }
    }
}
