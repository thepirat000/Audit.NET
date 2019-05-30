using System;
using Audit.Core;
using NLog;

namespace Audit.NLog.Configuration
{
    /// <summary>
    /// Provides a fluent API to configure the NLog data provider
    /// </summary>
    public interface INLogConfigurator
    {
        /// <summary>
        /// Sets the NLog logger (ILogger) to use as a function of the audit event.
        /// </summary>
        /// <param name="loggerBuilder">A way to obtain the NLog ILogger instance. Default is LogManager.GetLogger(auditEvent.GetType()).</param>
        INLogConfigurator Logger(Func<AuditEvent, ILogger> loggerBuilder);
        /// <summary>
        /// Sets a NLog logger (ILogger) to use for all the audit events.
        /// </summary>
        /// <param name="logger">The NLog ILogger instance.</param>
        INLogConfigurator Logger(ILogger logger);
        /// <summary>
        /// Sets the NLog log level to use as a function of the audit event.
        /// </summary>
        /// <param name="logLevelBuilder">A way to obtain the log level for the audit events.</param>
        INLogConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder);
        /// <summary>
        /// Sets the NLog log level to use for all the audit events.
        /// </summary>
        /// <param name="logLevel">The log level for the audit events.</param>
        INLogConfigurator LogLevel(LogLevel logLevel);
        /// <summary>
        /// Sets the message to log on NLog as a function of the audit event and and the eventid.
        /// Default is the AuditEvent JSON including the EventId as a custom field.
        /// </summary>
        /// <param name="messageBuilder">A way to obtain the object to be logged. Default is the AuditEvent JSON including the EventId as a custom field.</param>
        void Message(Func<AuditEvent, object, object> messageBuilder);
        /// <summary>
        /// Sets the message to log on NLog as a function of the audit event.
        /// Default is the AuditEvent JSON including the EventId as a custom field.
        /// </summary>
        /// <param name="messageBuilder">A way to obtain the object to be logged.</param>
        void Message(Func<AuditEvent, object> messageBuilder);
    }
}