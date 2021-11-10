using System;
using NLog;
using Audit.Core;

namespace Audit.NLog.Configuration
{
    public class NLogConfigurator : INLogConfigurator
    {
        internal Func<AuditEvent, ILogger> _loggerBuilder;
        internal Func<AuditEvent, LogLevel> _logLevelBuilder;
        internal Func<AuditEvent, object, object> _messageBuilder;

        public INLogConfigurator Logger(Func<AuditEvent, ILogger> loggerBuilder)
        {
            _loggerBuilder = loggerBuilder;
            return this;
        }

        public INLogConfigurator Logger(ILogger logger)
        {
            _loggerBuilder = _ => logger;
            return this;
        }

        public INLogConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder)
        {
            _logLevelBuilder = logLevelBuilder;
            return this;
        }

        public INLogConfigurator LogLevel(LogLevel logLevel)
        {
            _logLevelBuilder = _ => logLevel;
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