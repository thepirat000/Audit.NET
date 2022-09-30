namespace Audit.Serilog.Configuration
{
    using System;
    using global::Serilog;
    using Audit.Core;
    using Audit.NET.Serilog;


    public class SerilogConfigurator : ISerilogConfigurator
    {
        internal Func<AuditEvent, ILogger> _loggerBuilder;
        internal Func<AuditEvent, LogLevel> _logLevelBuilder;
        internal Func<AuditEvent, object, object> _messageBuilder;

        /// <inheritdoc />
        public ISerilogConfigurator Logger(Func<AuditEvent, ILogger> loggerBuilder)
        {
            _loggerBuilder = loggerBuilder;
            return this;
        }

        /// <inheritdoc />
        public ISerilogConfigurator Logger(ILogger logger)
        {
            _loggerBuilder = _ => logger;
            return this;
        }

        /// <inheritdoc />
        public ISerilogConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder)
        {
            _logLevelBuilder = logLevelBuilder;
            return this;
        }

        /// <inheritdoc />
        public ISerilogConfigurator LogLevel(LogLevel logLevel)
        {
            _logLevelBuilder = _ => logLevel;
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