using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LibSudo.Win32;
using SvcGuest.Logging;

namespace SvcGuest.ProgramWrappers
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
        private readonly DeepDarkWin32Fantasy.SafeTokenHandle _identityToken;

        private bool isChildProcessAlive;
        private Task childProcessWaitTask;
        public int ChildProcessId => _pi.dwProcessId;
        private Process _p;
        public Process P => IfChildProcessAlive() ? _p : null;

        private const int checkInterval = 1000;

        //private readonly System.Timers.Timer _checkChildProcessTimer = new System.Timers.Timer()
        //{
        //    AutoReset = true,
        //    Enabled = true,
        //    Interval = checkInterval,
        //};

        public NativeProgramWrapper(string launchType, int launchIndex, DeepDarkWin32Fantasy.SafeTokenHandle identityToken)
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
            isChildProcessAlive = true;
            LogMuxer.Instance.Debug($"Helper process at {ChildProcessId}");
            _p = Process.GetProcessById(ChildProcessId);
            // _checkChildProcessTimer.Elapsed += OnCheckChildProcessTimer;
            childProcessWaitTask = Task.Run(() => WaitForExitInternal());
        }

        public override void Stop()
        {
            if (HasExited) return;
            if (!IfChildProcessAlive()) return;
            QuitProcess(_p);
        }

        private void WaitForExitInternal()
        {
            while (isChildProcessAlive)
            {
                try
                {
                    if (Kernel32.WaitForSingleObject((IntPtr)ChildProcessId, DeepDarkWin32Fantasy.INFINITE) == DeepDarkWin32Fantasy.WAIT_OBJECT_0)
                    {
                        // Signaled            
                        isChildProcessAlive = false;

                    }
                    else
                    {
                        Thread.Sleep(checkInterval);
                    }
                }

                catch (Exception Ex)
                {
                    // An exception occurred
                    LogMuxer.Instance.Error(Ex.ToString());
                }
            }
        }

        public override void WaitForExit()
        {
            // old impl: use WMI
            // if (IfChildProcessAlive()) _p.WaitForExit();

            // new impl: use WaitForSingleObject
            childProcessWaitTask.Wait();
        }

        private bool IfChildProcessAlive()
        {
            // old impl
            // return GetChildProcessIds(SelfProcessId).Contains(ChildProcessId);

            // new impl
            return isChildProcessAlive;
        }

        //private void OnCheckChildProcessTimer(object sender, EventArgs e)
        //{
        //    if (!IfChildProcessAlive())
        //    {
        //        OnProgramExited(this, null);
        //        _checkChildProcessTimer.Enabled = false;
        //    }
        //}
    }
}
