using System;
using Audit.Core;
using log4net;

namespace Audit.log4net.Configuration
{
    public class Log4netConfigurator : ILog4netConfigurator
    {
        internal Setting<ILog> _logger;
        internal Setting<LogLevel?> _logLevel;
        internal Func<AuditEvent, object, object> _messageBuilder;

        public ILog4netConfigurator Logger(Func<AuditEvent, ILog> loggerBuilder)
        {
            _logger = loggerBuilder;
            return this;
        }

        public ILog4netConfigurator Logger(ILog logger)
        {
            _logger = new Setting<ILog>(logger);
            return this;
        }

        public ILog4netConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder)
        {
            _logLevel = new Setting<LogLevel?>(ev => logLevelBuilder.Invoke(ev));
            return this;
        }

        public ILog4netConfigurator LogLevel(LogLevel logLevel)
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