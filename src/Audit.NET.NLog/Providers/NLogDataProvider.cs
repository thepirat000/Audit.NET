using System;
using NLog;
using Audit.Core;

namespace Audit.NLog.Providers
{
    /// <summary>
    /// Store Audit logs using NLog.
    /// </summary>
    /// <remarks>
    /// Settings:
    ///     Logger/LoggerBuilder: A way to obtain the NLog ILog instance. Default is LogManager.GetLogger(auditEvent.GetType()).
    ///     LogLevel/LogLevelBuilder: A way to obtain the log level for the audit events. Default is Error for exceptions and Info for anything else.
    ///     LogBodyBuilder: A way to obtain the object to be logged. Default is the AuditEvent JSON including the EventId as a custom field.
    /// </remarks>
    public class NLogDataProvider : AuditDataProvider
    {
        /// <summary>
        /// A function that given an audit event returns the NLog ILog implementation to use
        /// </summary>
        public Func<AuditEvent, ILogger> LoggerBuilder { get; set; }
        /// <summary>
        /// The NLog ILog implementation to use
        /// </summary>
        public ILogger Logger { set { LoggerBuilder = _ => value; } }
        /// <summary>
        /// A function that given an audit event returns the NLog Log Level to use
        /// </summary>
        public Func<AuditEvent, LogLevel> LogLevelBuilder { get; set; }
        /// <summary>
        /// The NLog Log Level to use
        /// </summary>
        public LogLevel LogLevel { set { LogLevelBuilder = _ => value; } }
        /// <summary>
        /// A function that given an audit event and an event id, returns the message to log
        /// </summary>
        public Func<AuditEvent, object, object> LogMessageBuilder { get; set; }

        public NLogDataProvider()
        {
        }

        public NLogDataProvider(Action<Configuration.INLogConfigurator> config)
        {
            var logConfig = new Configuration.NLogConfigurator();
            if (config != null)
            {
                config.Invoke(logConfig);
                LoggerBuilder = logConfig._loggerBuilder;
                LogLevelBuilder = logConfig._logLevelBuilder;
                LogMessageBuilder = logConfig._messageBuilder;
            }
        }

        private ILogger GetLogger(AuditEvent auditEvent)
        {
            return LoggerBuilder?.Invoke(auditEvent) ?? LogManager.GetLogger(auditEvent.GetType().FullName);
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
        /// Stores an event via NLog
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            Log(auditEvent, eventId);
            return eventId;
        }

        /// <summary>
        /// Stores an event related to a previous event, via NLog
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            Log(auditEvent, eventId);
        }
    }
}