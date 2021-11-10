using System;
using Audit.Core;
using log4net;

namespace Audit.log4net.Configuration
{
    /// <summary>
    /// Provides a fluent API to configure the Log4net data provider
    /// </summary>
    public interface ILog4netConfigurator
    {
        /// <summary>
        /// Sets the log4net logger (ILog) to use as a function of the audit event.
        /// </summary>
        /// <param name="loggerBuilder">A way to obtain the log4net ILog instance. Default is LogManager.GetLogger(auditEvent.GetType()).</param>
        ILog4netConfigurator Logger(Func<AuditEvent, ILog> loggerBuilder);
        /// <summary>
        /// Sets a log4net logger (ILog) to use for all the audit events.
        /// </summary>
        /// <param name="logger">The log4net ILog instance.</param>
        ILog4netConfigurator Logger(ILog logger);
        /// <summary>
        /// Sets the log4net log level to use as a function of the audit event.
        /// </summary>
        /// <param name="logLevelBuilder">A way to obtain the log level for the audit events.</param>
        ILog4netConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder);
        /// <summary>
        /// Sets the log4net log level to use for all the audit events.
        /// </summary>
        /// <param name="logLevel">The log level for the audit events.</param>
        ILog4netConfigurator LogLevel(LogLevel logLevel);
        /// <summary>
        /// Sets the message to log on log4net as a function of the audit event and and the eventid.
        /// Default is the AuditEvent JSON including the EventId as a custom field.
        /// </summary>
        /// <param name="messageBuilder">A way to obtain the object to be logged. Default is the AuditEvent JSON including the EventId as a custom field.</param>
        void Message(Func<AuditEvent, object, object> messageBuilder);
        /// <summary>
        /// Sets the message to log on log4net as a function of the audit event.
        /// Default is the AuditEvent JSON including the EventId as a custom field.
        /// </summary>
        /// <param name="messageBuilder">A way to obtain the object to be logged.</param>
        void Message(Func<AuditEvent, object> messageBuilder);
    }
}