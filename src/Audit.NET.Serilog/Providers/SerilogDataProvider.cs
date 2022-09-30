namespace Audit.NET.Serilog.Providers
{
    using System;
    using Audit.Core;
    using Audit.Serilog.Configuration;
    using global::Serilog;


    /// <summary>
    ///     Store Audit logs using Serilog.
    /// </summary>
    /// <remarks>
    ///     Settings:
    ///     Logger/LoggerBuilder: A way to obtain the Serilog ILog instance. Default is
    ///     LogManager.GetLogger(auditEvent.GetType()).
    ///     LogLevel/LogLevelBuilder: A way to obtain the log level for the audit events. Default is Error for exceptions and
    ///     Info for anything else.
    ///     LogBodyBuilder: A way to obtain the object to be logged. Default is the AuditEvent JSON including the EventId as a
    ///     custom field.
    /// </remarks>
    public class SerilogDataProvider : AuditDataProvider
    {
        /// <summary>
        /// A function that given an audit event returns the Serilog ILog implementation to use
        /// </summary>
        public Func<AuditEvent, ILogger> LoggerBuilder { get; set; }
        /// <summary>
        /// The Serilog ILog implementation to use
        /// </summary>
        public ILogger Logger { set { LoggerBuilder = _ => value; } }
        /// <summary>
        /// A function that given an audit event returns the Serilog Log Level to use
        /// </summary>
        public Func<AuditEvent, LogLevel> LogLevelBuilder { get; set; }
        /// <summary>
        /// The Serilog Log Level to use
        /// </summary>
        public LogLevel LogLevel { set { LogLevelBuilder = _ => value; } }
        /// <summary>
        /// A function that given an audit event and an event id, returns the message to log
        /// </summary>
        public Func<AuditEvent, object, object> LogMessageBuilder { get; set; }
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="SerilogDataProvider" /> class.
        /// </summary>
        public SerilogDataProvider()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SerilogDataProvider" /> class.
        /// </summary>
        /// <param name="config">Configurator instance.</param>
        public SerilogDataProvider(Action<ISerilogConfigurator> config)
        {
            var logConfig = new SerilogConfigurator();
            if (config == null)
            {
                return;
            }

            config.Invoke(logConfig);
            LoggerBuilder = logConfig._loggerBuilder;
            LogLevelBuilder = logConfig._logLevelBuilder;
            LogMessageBuilder = logConfig._messageBuilder;
        }

        /// <summary>
        ///     Stores an event via Serilog.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <returns>Event Id.</returns>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            this.Log(auditEvent, eventId);
            return eventId;
        }

        /// <summary>
        ///     Stores an event related to a previous event, via Serilog.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="auditEvent">The audit event.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            this.Log(auditEvent, eventId);
        }

        private ILogger GetLogger(AuditEvent auditEvent)
        {
            return this.LoggerBuilder?.Invoke(auditEvent) ?? global::Serilog.Log.ForContext(auditEvent.GetType());
        }

        private LogLevel GetLogLevel(AuditEvent auditEvent)
        {
            return this.LogLevelBuilder?.Invoke(auditEvent) ??
                   (auditEvent.Environment.Exception != null ? LogLevel.Error : LogLevel.Info);
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
            LogLevel level = GetLogLevel(auditEvent);
            var value = GetLogObject(auditEvent, eventId);

            switch (level)
            {
                case LogLevel.Debug:
                    logger.Debug("{Value}", value);
                    break;
                case LogLevel.Warn:
                    logger.Warning("{Value}", value);
                    break;
                case LogLevel.Error:
                    logger.Error("{Value}", value);
                    break;
                case LogLevel.Fatal:
                    logger.Fatal("{Value}", value);
                    break;
                case LogLevel.Info:
                    logger.Information("{Value}", value);
                    break;
                default:
                    logger.Information("{Value}", value);
                    break;
            }
        }
    }
}