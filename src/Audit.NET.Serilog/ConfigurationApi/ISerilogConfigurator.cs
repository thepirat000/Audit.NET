namespace Audit.Serilog.Configuration
{
    using Audit.NET.Serilog;
    using System;
    using Audit.Core;
    using global::Serilog;

    /// <summary>
    ///     Provides a fluent API to configure the Serilog data provider.
    /// </summary>
    public interface ISerilogConfigurator
    {
        /// <summary>
        ///     Sets the Serilog logger (ILog) to use as a function of the audit event.
        /// </summary>
        /// <param name="loggerBuilder">
        ///     A way to obtain the Serilog ILog instance. Default is
        ///     LogManager.GetLogger(auditEvent.GetType()).
        /// </param>
        /// <returns>Configurator.</returns>
        ISerilogConfigurator Logger(Func<AuditEvent, ILogger> loggerBuilder);

        /// <summary>
        ///     Sets a Serilog logger (ILog) to use for all the audit events.
        /// </summary>
        /// <param name="logger">The Serilog ILog instance.</param>
        /// <returns>Configurator.</returns>
        ISerilogConfigurator Logger(ILogger logger);

        /// <summary>
        ///     Sets the Serilog log level to use as a function of the audit event.
        /// </summary>
        /// <param name="logLevelBuilder">A way to obtain the log level for the audit events.</param>
        /// <returns>Configurator.</returns>
        ISerilogConfigurator LogLevel(Func<AuditEvent, LogLevel> logLevelBuilder);

        /// <summary>
        ///     Sets the Serilog log level to use for all the audit events.
        /// </summary>
        /// <param name="logLevel">The log level for the audit events.</param>
        /// <returns>Configurator.</returns>
        ISerilogConfigurator LogLevel(LogLevel logLevel);

        /// <summary>
        ///     Sets the message to log on Serilog as a function of the audit event and and the eventid.
        ///     Default is the AuditEvent JSON including the EventId as a custom field.
        /// </summary>
        /// <param name="messageBuilder">
        ///     A way to obtain the object to be logged. Default is the AuditEvent JSON including the
        ///     EventId as a custom field.
        /// </param>
        void Message(Func<AuditEvent, object, object> messageBuilder);

        /// <summary>
        ///     Sets the message to log on Serilog as a function of the audit event.
        ///     Default is the AuditEvent JSON including the EventId as a custom field.
        /// </summary>
        /// <param name="messageBuilder">A way to obtain the object to be logged.</param>
        void Message(Func<AuditEvent, object> messageBuilder);
    }
}