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
    ///     Logger/LoggerBuilder: A way to obtain the log4net ILog instance. Default is LogManager.GetLogger(auditEvent.GetType()).
    ///     LogLevel/LogLevelBuilder: A way to obtain the log level for the audit events.
    ///     LogBodyBuilder: A way to obtain the object to be logged. Default is the AuditEvent JSON including the EventId as a custom field.
    /// </remarks>
    public class Log4netDataProvider : AuditDataProvider
    {
        /// <summary>
        /// A function that given an audit event returns the log4net ILog implementation to use
        /// </summary>
        public Func<AuditEvent, ILog> LoggerBuilder { get; set; }
        /// <summary>
        /// The log4net ILog implementation to use
        /// </summary>
        public ILog Logger { set { LoggerBuilder = _ => value; } }
        /// <summary>
        /// A function that given an audit event returns the log4net Log Level to use
        /// </summary>
        public Func<AuditEvent, LogLevel> LogLevelBuilder { get; set; }
        /// <summary>
        /// The log4net Log Level to use
        /// </summary>
        public LogLevel LogLevel { set { LogLevelBuilder = _ => value; } }
        /// <summary>
        /// A function that given an audit event and an event id, returns the message to log
        /// </summary>
        public Func<AuditEvent, object, object> LogMessageBuilder { get; set; }

        private ILog GetLogger(AuditEvent auditEvent)
        {
            return LoggerBuilder == null ? LogManager.GetLogger(auditEvent.GetType()) : LoggerBuilder.Invoke(auditEvent);
        }
        private LogLevel GetLogLevel(AuditEvent auditEvent)
        {
            return LogLevelBuilder?.Invoke(auditEvent) ?? (auditEvent.Environment.Exception != null ? LogLevel.Error : LogLevel.Info);
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
                case LogLevel.Debug:
                    logger.Debug(value);
                    break;
                case LogLevel.Warn:
                    logger.Warn(value);
                    break;
                case LogLevel.Error:
                    logger.Error(value);
                    break;
                case LogLevel.Fatal:
                    logger.Fatal(value);
                    break;
                case LogLevel.Info:
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