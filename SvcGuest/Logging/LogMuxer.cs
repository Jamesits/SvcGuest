using System.Collections.Generic;

namespace SvcGuest.Logging
{
    internal class LogMuxer : Logger
    {

        public static LogMuxer Instance { get; } = new LogMuxer();

        private readonly List<Logger> _loggers = new List<Logger>()
        {
            new DebugLogger(),
            new ConsoleLogger(),
        };

        public LogMuxer()
        {
            try
            {
                _loggers.Add(new LegacyEventLogger());
            }
            catch
            {
                // I'm fine
                Instance.Warning("Unable to attach a LegacyEventLogger, maybe because lack of privilege?");
            }
        }

        internal override void Debug(string s)
        {
            #if DEBUG
            foreach (var logger in _loggers)
            {
                logger.Debug(s);
            }
            #endif
        }

        internal override void Info(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.Info(s);
            }
        }

        internal override void Warning(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.Warning(s);
            }
        }

        internal override void Error(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.Error(s);
            }
        }

        internal override void Fatal(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.Fatal(s);
            }
        }

        internal override void SuccessAudit(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.SuccessAudit(s);
            }
        }

        internal override void FailureAudit(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.FailureAudit(s);
            }
        }

        internal override void SubprocessStdout(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.SubprocessStdout(s);
            }
        }

        internal override void SubprocessStderr(string s)
        {
            foreach (var logger in _loggers)
            {
                logger.SubprocessStderr(s);
            }
        }

        internal override void Flush()
        {
            foreach (var logger in _loggers)
            {
                logger.Flush();
            }
        }
    }
}
