using System;
using System.Diagnostics;
using System.Security;
using System.Timers;

namespace SvcGuest.Logging
{
    internal class LegacyEventLogger : Logger
    {
        protected static string EventSourceName => Globals.ServiceName ?? "SvcGuest";
        protected const string EventCategory = "Application";

        protected const int LogMergeWindow = 5; // seconds


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

        protected void OnMessage(string message, bool isStderr)
        {
            if (LastLogEntry == null)
            {
                LastLogEntry = new LogBufferEntry()
                {
                    Count = 1,
                    Message = message,
                    Type = isStderr ? EventLogEntryType.Error : EventLogEntryType.Information,
                    LastHitTime = DateTime.Now,
                };
            }
            else
            {
                if (string.Equals(LastLogEntry.Message, message, StringComparison.Ordinal))
                {
                    LastLogEntry.Count++;
                    LastLogEntry.LastHitTime = DateTime.Now;
                }
                else
                {
                    FlushLogBufferTimer.Enabled = false;
                    CommitLog();
                    FlushLogBufferTimer.Enabled = true;
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
            System.Diagnostics.Debug.WriteLine("Commiting log");
            if (LastLogEntry == null) return;
            if (LastLogEntry.Count > 1)
            {
                EventLog.WriteEntry(EventSourceName,
                    $"[Repeated {LastLogEntry.Count} times in {(DateTime.Now - LastLogEntry.LastHitTime).TotalSeconds} seconds] {LastLogEntry.Message}",
                    LastLogEntry.Type);
            }
            else
            {
                EventLog.WriteEntry(EventSourceName, LastLogEntry.Message, LastLogEntry.Type);
            }

            LastLogEntry = null;
        }


        internal LegacyEventLogger()
        {
            // initialize event log
            try
            {
                if (!EventLog.SourceExists(EventSourceName))
                    EventLog.CreateEventSource(EventSourceName, EventCategory);
            }
            catch (SecurityException)
            {
                // do not have privilege
                System.Diagnostics.Debug.WriteLine("Insufficient privilege to create or query EventLog");
                throw;
            }
            catch (ArgumentException)
            {
                // already created
                System.Diagnostics.Debug.WriteLine("The required EventLog is already created but check for its existence failed");
                throw;
            }

            // set up flush log timer
            FlushLogBufferTimer.Elapsed += OnLogBufferFlushTimer;
        }

        internal override void Debug(string s) { }
        internal override void Info(string s) => EventLog.WriteEntry(EventSourceName, s, EventLogEntryType.Information);
        internal override void Warning(string s) => EventLog.WriteEntry(EventSourceName, s, EventLogEntryType.Warning);
        internal override void Error(string s) => EventLog.WriteEntry(EventSourceName, s, EventLogEntryType.Error);
        internal override void Fatal(string s) => EventLog.WriteEntry(EventSourceName, s, EventLogEntryType.Error);
        internal override void SuccessAudit(string s) => EventLog.WriteEntry(EventSourceName, s, EventLogEntryType.SuccessAudit);
        internal override void FailureAudit(string s) => EventLog.WriteEntry(EventSourceName, s, EventLogEntryType.FailureAudit);
        internal override void SubprocessStdout(string s) => OnMessage(s, false);
        internal override void SubprocessStderr(string s) => OnMessage(s, true);

        internal override void Flush() => OnLogBufferFlushTimer(this, null);
    }
}
