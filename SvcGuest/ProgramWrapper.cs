using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace SvcGuest
{
    abstract class ProgramWrapper
    {
        protected class LogBufferEntry
        {
            public string Message { get; set; }
            public uint Count { get; set; }
            public EventLogEntryType Type { get; set; }
            public DateTime LastHitTime { get; set; }
        }

        protected LogBufferEntry LastLogEntry;

        protected readonly Timer FlushLogBufferTimer = new Timer()
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000,
        };

        public const int KillWaitMs = 20000;
        protected const int LogMergeWindow = 5; // seconds
        protected readonly string EventSourceName = Globals.ServiceName;
        protected const string EventCategory = "Application";

        protected virtual void OnProgramExited(object sender, EventArgs e)
        {
            EventHandler handler = ProgramExited;
            handler?.Invoke(sender, e);
        }

        public event EventHandler ProgramExited;

        protected ProgramWrapper() 
        {
            // initialize event log
            if (!EventLog.SourceExists(EventSourceName))
                EventLog.CreateEventSource(EventSourceName, EventCategory);

            // set up flush log timer
            FlushLogBufferTimer.Elapsed += OnLogBufferFlushTimer;
        }

        public abstract void Start();

        public abstract void Stop();

        protected void QuitProcess(Process p)
        {
            if (p.HasExited)
            {
                EventLog.WriteEntry(EventSourceName, "Main process already exited", EventLogEntryType.FailureAudit);
                return;
            }

            try
            {
                EventLog.WriteEntry(EventSourceName, "Terminating main process", EventLogEntryType.Information);
                if (p.CloseMainWindow())
                {
                    Debug.WriteLine("Child process has a window, close message sent");
                    p.WaitForExit(KillWaitMs);
                }
                else
                {
                    // explanation see: http://stanislavs.org/stopping-command-line-applications-programatically-with-ctrl-c-events-from-net/
                    // try to attach to process' console
                    if (Kernel32.AttachConsole((uint)p.Id))
                    {
                        Debug.WriteLine("Child process has a console, trying to send a ^C");

                        // Disable Ctrl-C handling for our program
                        Kernel32.SetConsoleCtrlHandler(null, true);

                        // Sent Ctrl-C to the attached console
                        Kernel32.GenerateConsoleCtrlEvent(Kernel32.CtrlTypes.CTRL_C_EVENT, 0);

                        // Must wait here. If we don't wait and re-enable Ctrl-C handling below too fast, we might terminate ourselves.
                        p.WaitForExit(KillWaitMs);

                        Kernel32.FreeConsole();
                        Kernel32.SetConsoleCtrlHandler(null, false);
                    }
                }
                if (!p.HasExited)
                {
                    // failed to terminate
                    EventLog.WriteEntry(EventSourceName, "Main process failed to gracefully shutdown", EventLogEntryType.FailureAudit);
                    Debug.WriteLine("Child process failed to terminate, killing");
                    p.Kill();
                    p.WaitForExit();
                }
            }
            catch (InvalidOperationException)
            {
                // already exited
                EventLog.WriteEntry(EventSourceName, "Unable to signal main process for termination, maybe exited already", EventLogEntryType.FailureAudit);
            }
        }

        // channel = false => stdout
        // channel = true => stderr
        protected void OnMessage(string message, bool channel)
        {
            if (LastLogEntry == null)
            {
                LastLogEntry = new LogBufferEntry()
                {
                    Count = 1,
                    Message = message,
                    Type = channel ? EventLogEntryType.Error : EventLogEntryType.Information,
                    LastHitTime = DateTime.Now,
                };
            }
            else
            {
                if (LastLogEntry.Message == message)
                {
                    LastLogEntry.Count++;
                    LastLogEntry.LastHitTime = DateTime.Now;
                }
                else
                {
                    CommitLog();
                }
            }

        }

        private void OnLogBufferFlushTimer(Object sender, EventArgs args)
        {
            if ((DateTime.Now - LastLogEntry.LastHitTime).TotalSeconds > LogMergeWindow)
            {
                CommitLog();
            }
        }

        protected void CommitLog()
        {
            if (LastLogEntry != null)
            {
                EventLog.WriteEntry(EventSourceName,
                    LastLogEntry.Count > 1
                        ? $"[Repeated {LastLogEntry.Count} times in {(DateTime.Now - LastLogEntry.LastHitTime).TotalSeconds} seconds] {LastLogEntry.Message}"
                        : LastLogEntry.Message, LastLogEntry.Type);

                LastLogEntry = null;
            }
        }
    }
}