using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using SvcGuest.Logging;
using SvcGuest.Win32;
using Timer = System.Timers.Timer;

namespace SvcGuest.ProgramWrappers
{
    /// <summary>
    /// Unified interface for starting and stopping an external program.
    /// Also provides basic functions for logging.
    /// </summary>
    public abstract class ProgramWrapper
    {
        #region Logging
        
        // channel = false => stdout
        // channel = true => stderr
        
        #endregion

        public const int KillWaitMs = 20000;

        #region interfaces
        public static int SelfProcessId => Process.GetCurrentProcess().Id;

        protected virtual void OnProgramExited(object sender, EventArgs e)
        {
            _hasExited = true;
            EventHandler handler = ProgramExited;
            handler?.Invoke(sender, e);
        }

        public event EventHandler ProgramExited;

        private bool _hasExited;
        public bool HasExited { get => _hasExited; }

        public abstract void Start();

        public abstract void Stop();
        #endregion

        protected ProgramWrapper() { }

        public virtual void WaitForExit()
        {
            throw new NotImplementedException();
        }
  
        public static void QuitProcess(Process p)
        {
            if (p.HasExited)
            {
                LogMuxer.Instance.Debug("Subprocess already exited, no need to quit process");
                return;
            }

            try
            {
                LogMuxer.Instance.Info("Terminating subprocess");
                if (p.CloseMainWindow())
                {
                    LogMuxer.Instance.Debug("Subprocess has a window, close message sent");
                    p.WaitForExit(KillWaitMs);
                }
                else
                {
                    // explanation see: http://stanislavs.org/stopping-command-line-applications-programatically-with-ctrl-c-events-from-net/
                    // try to attach to process' console
                    if (Kernel32.AttachConsole((uint)p.Id))
                    {
                        LogMuxer.Instance.Debug("Subprocess has a console, trying to send a ^C");

                        // Disable Ctrl-C handling for our program
                        Kernel32.SetConsoleCtrlHandler(null, true);

                        // Sent Ctrl-C to the attached console
                        Kernel32.GenerateConsoleCtrlEvent(DeepDarkWin32Fantasy.CtrlTypes.CTRL_C_EVENT, 0);

                        // Must wait here. If we don't wait and re-enable Ctrl-C handling below too fast, we might terminate ourselves.
                        p.WaitForExit(KillWaitMs);

                        Kernel32.FreeConsole();
                        Kernel32.SetConsoleCtrlHandler(null, false);
                    }
                }
                if (!p.HasExited)
                {
                    // failed to terminate
                    LogMuxer.Instance.Warning("Subprocess failed to gracefully shutdown, killing");
                    p.Kill();
                    p.WaitForExit();
                }
            }
            catch (InvalidOperationException)
            {
                // already exited
                LogMuxer.Instance.Warning("Unable to signal main process for termination, maybe exited already");
            }
        }

        public static IEnumerable<int> GetChildProcessIds(int processId)
        {
            List<int> children = new List<int>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={processId}");

            foreach (var o in mos.Get())
            {
                var mo = (ManagementObject)o;
                children.Add(Convert.ToInt32(mo["ProcessID"]));
            }

            children = children.OrderByDescending(i => i).ToList();

            return children;
        }
    }
}