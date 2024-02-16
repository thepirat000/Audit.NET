namespace Audit.Serilog.Configuration
{
    using System;
    using global::Serilog;
    using Audit.Core;

    public class SerilogConfigurator : ISerilogConfigurator
    {
        internal Setting<ILogger> _logger;
        internal Setting<LogLevel?> _logLevel;
        internal Func<AuditEvent, object, object> _messageBuilder;

        /// <inheritdoc />
        public ISerilogConfigurator Logger(Func<AuditEvent, ILogger> loggerBuilder)
        {
            _logger = loggerBuilder;
            return this;
        }

        /// <inheritdoc />
        public ISerilogConfigurator Logger(ILogger logger)
        {
            _logger = new Setting<ILogger>(logger);
            return this;
        }

        /// <inheritdoc />
        public ISerilogConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder)
        {
            _logLevel = new Setting<LogLevel?>(ev => logLevelBuilder(ev));
            return this;
        }

        /// <inheritdoc />
        public ISerilogConfigurator LogLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        /// <inheritdoc />
        public void Message(Func<AuditEvent, object, object> messageBuilder)
        {
            _messageBuilder = messageBuilder;
        }

        /// <inheritdoc />
        public void Message(Func<AuditEvent, object> messageBuilder)
        {
            _messageBuilder = (ev, _) => messageBuilder.Invoke(ev);
        }
    }
}