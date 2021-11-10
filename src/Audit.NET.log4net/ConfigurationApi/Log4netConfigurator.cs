using System;
using Audit.Core;
using log4net;

namespace Audit.log4net.Configuration
{
    public class Log4netConfigurator : ILog4netConfigurator
    {
        internal Func<AuditEvent, ILog> _loggerBuilder;
        internal Func<AuditEvent, LogLevel> _logLevelBuilder;
        internal Func<AuditEvent, object, object> _messageBuilder;

        public ILog4netConfigurator Logger(Func<AuditEvent, ILog> loggerBuilder)
        {
            _loggerBuilder = loggerBuilder;
            return this;
        }

        public ILog4netConfigurator Logger(ILog logger)
        {
            _loggerBuilder = _ => logger;
            return this;
        }

        public ILog4netConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder)
        {
            _logLevelBuilder = logLevelBuilder;
            return this;
        }

        public ILog4netConfigurator LogLevel(LogLevel logLevel)
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