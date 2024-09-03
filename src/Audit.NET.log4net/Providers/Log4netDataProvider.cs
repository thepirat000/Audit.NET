using System;
using Audit.Core;
using log4net;

namespace Audit.log4net.Providers
{
    /// <summary>
    /// Store Audit logs using log4net.
    /// </summary>
    /// <remarks>
    /// Settings:
    ///     Logger: A way to obtain the log4net ILog instance. Default is LogManager.GetLogger(auditEvent.GetType()).
    ///     LogLevel: A way to obtain the log level for the audit events.
    ///     LogMessageBuilder: A way to obtain the object to be logged. Default is the AuditEvent JSON including the EventId as a custom field.
    /// </remarks>
    public class Log4netDataProvider : AuditDataProvider
    {
        /// <summary>
        /// The log4net ILog implementation to use
        /// </summary>
        public Setting<ILog> Logger { get; set; }
        /// <summary>
        /// The log4net Log Level to use
        /// </summary>
        public Setting<LogLevel?> LogLevel { get; set; }
        /// <summary>
        /// A function that given an audit event and an event id, returns the message to log
        /// </summary>
        public Func<AuditEvent, object, object> LogMessageBuilder { get; set; }

        public Log4netDataProvider()
        {
        }

        public Log4netDataProvider(Action<Configuration.ILog4netConfigurator> config)
        {
            var logConfig = new Configuration.Log4netConfigurator();
            if (config != null)
            {
                config.Invoke(logConfig);
                Logger = logConfig._logger;
                LogLevel = logConfig._logLevel;
                LogMessageBuilder = logConfig._messageBuilder;
            }
        }

        private ILog GetLogger(AuditEvent auditEvent)
        {
            return Logger.GetValue(auditEvent) ?? LogManager.GetLogger(auditEvent.GetType());
        }
        
        private LogLevel GetLogLevel(AuditEvent auditEvent)
        {
            return LogLevel.GetValue(auditEvent) ?? (auditEvent.Environment?.Exception != null ? log4net.LogLevel.Error : log4net.LogLevel.Info);
        }

        private object GetLogObject(AuditEvent auditEvent, object eventId)
        {
            if (LogMessageBuilder == null)
            {
                if (eventId != null)
                {
                    auditEvent.CustomFields["EventId"] = eventId;
                }
                return auditEvent.ToJson();
            }
            return LogMessageBuilder.Invoke(auditEvent, eventId);
        }

        private void Log(AuditEvent auditEvent, object eventId)
        {
            var logger = GetLogger(auditEvent);
            var level = GetLogLevel(auditEvent);
            var value = GetLogObject(auditEvent, eventId);
            switch (level)
            {
                case log4net.LogLevel.Debug:
                    logger.Debug(value);
                    break;
                case log4net.LogLevel.Warn:
                    logger.Warn(value);
                    break;
                case log4net.LogLevel.Error:
                    logger.Error(value);
                    break;
                case log4net.LogLevel.Fatal:
                    logger.Fatal(value);
                    break;
                default:
                    logger.Info(value);
                    break;
            }
        }

        /// <summary>
        /// Stores an event via log4net
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            Log(auditEvent, eventId);
            return eventId;
        }

        /// <summary>
        /// Stores an event related to a previous event, via log4net
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            Log(auditEvent, eventId);
        }
    }
}