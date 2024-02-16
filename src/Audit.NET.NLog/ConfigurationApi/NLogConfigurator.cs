using System;
using NLog;
using Audit.Core;

namespace Audit.NLog.Configuration
{
    public class NLogConfigurator : INLogConfigurator
    {
        internal Setting<ILogger> _logger;
        internal Setting<LogLevel?> _logLevel;
        internal Func<AuditEvent, object, object> _messageBuilder;

        public INLogConfigurator Logger(Func<AuditEvent, ILogger> loggerBuilder)
        {
            _logger = loggerBuilder;
            return this;
        }

        public INLogConfigurator Logger(ILogger logger)
        {
            _logger  = new Setting<ILogger>(logger);
            return this;
        }

        public INLogConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder)
        {
            _logLevel = new Setting<LogLevel?>(ev => logLevelBuilder.Invoke(ev));
            return this;
        }

        public INLogConfigurator LogLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        public void Message(Func<AuditEvent, object, object> messageBuilder)
        {
            _messageBuilder = messageBuilder;
        }

        public void Message(Func<AuditEvent, object> messageBuilder)
        {
            _messageBuilder = (ev, _) => messageBuilder.Invoke(ev);
        }
    }
}