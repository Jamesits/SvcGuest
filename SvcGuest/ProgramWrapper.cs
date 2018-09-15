using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SvcGuest
{
    class ProgramWrapper
    {
        private class LogBufferEntry
        {
            public string Message { get; set; }
            public uint Count { get; set; }
            public EventLogEntryType Type { get; set; }
            public DateTime lastHitTime { get; set; }
        }
        private readonly Process _p = new Process();
        private LogBufferEntry _lastLogEntry;
        private readonly Timer _flushLogBufferTimer = new Timer()
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000,
        };

        private const int KillWaitMs = 20000;
        private const int LogMergeWindow = 5; // seconds
        private readonly string EventSourceName = Globals.ServiceName;
        private const string EventCategory = "Application";

        public ProgramWrapper(string executableName) : this(executableName, null)
        {

        }

        public ProgramWrapper(string executableName, string arguments)
        {
            // initialize event log
            if (!EventLog.SourceExists(EventSourceName))
                EventLog.CreateEventSource(EventSourceName, EventCategory);

            // set up flush log timer
            _flushLogBufferTimer.Elapsed += OnLogBufferFlushTimer;

            // initialize process
            _p.StartInfo.FileName = executableName;
            _p.StartInfo.Arguments = arguments;
            _p.StartInfo.UseShellExecute = false;
            _p.StartInfo.RedirectStandardOutput = true;
            _p.OutputDataReceived += (sender, args) => OnMessage(args.Data, false);
            _p.ErrorDataReceived += (sender, args) => OnMessage(args.Data, true);
            _p.Exited += (sender, args) => OnExit();
        }

        public void Start()
        {
            _p.Start();
            try
            {
                _p.BeginOutputReadLine();
                _p.BeginErrorReadLine();
            }
            catch (InvalidOperationException)
            {
                EventLog.WriteEntry(EventSourceName, "Unable to read stdout/stderr", EventLogEntryType.FailureAudit);
            }
        }

        public void Stop()
        {
            if (_p.HasExited)
            {
                EventLog.WriteEntry(EventSourceName, "Main process already exited", EventLogEntryType.FailureAudit);
                return;
            }
            try
            {
                EventLog.WriteEntry(EventSourceName, "Terminating main process", EventLogEntryType.Information);
                _p.CloseMainWindow();
                _p.WaitForExit(KillWaitMs);
                if (!_p.HasExited)
                {
                    // failed to terminate
                    EventLog.WriteEntry(EventSourceName, "Main process failed to gracefully shutdown", EventLogEntryType.FailureAudit);
                    _p.Kill();
                    _p.WaitForExit();
                }
            }
            catch (InvalidOperationException)
            {
                // already exited
                EventLog.WriteEntry(EventSourceName, "Unable to signal main process for termination, maybe exited already", EventLogEntryType.FailureAudit);
            }
            _p.Close();
        }

        // channel = false => stdout
        // channel = true => stderr
        private void OnMessage(string message, bool channel)
        {
            if (_lastLogEntry == null)
            {
                _lastLogEntry = new LogBufferEntry()
                {
                    Count = 1,
                    Message = message,
                    Type = channel ? EventLogEntryType.Error : EventLogEntryType.Information,
                    lastHitTime = DateTime.Now,
                };
            }
            else
            {
                if (_lastLogEntry.Message == message)
                {
                    _lastLogEntry.Count++;
                    _lastLogEntry.lastHitTime = DateTime.Now;
                }
                else
                {
                    CommitLog();
                }
            }

        }

        private void OnLogBufferFlushTimer(Object sender, EventArgs args)
        {
            if ((DateTime.Now - _lastLogEntry.lastHitTime).TotalSeconds > LogMergeWindow)
            {
                CommitLog();
            }
        }

        private void CommitLog()
        {
            if (_lastLogEntry != null)
            {
                if (_lastLogEntry.Count > 1)
                {
                    EventLog.WriteEntry(EventSourceName, $"[Repeated {_lastLogEntry.Count} times in {(DateTime.Now - _lastLogEntry.lastHitTime).TotalSeconds} seconds] {_lastLogEntry.Message}", _lastLogEntry.Type);
                }
                else
                {
                    EventLog.WriteEntry(EventSourceName, _lastLogEntry.Message, _lastLogEntry.Type);
                }

                _lastLogEntry = null;
            }
        }

        private void OnExit()
        {

        }
    }
}
