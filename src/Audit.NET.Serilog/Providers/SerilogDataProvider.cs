using System;

using Audit.Core;
using Audit.Serilog.Configuration;

using global::Serilog;

namespace Audit.Serilog.Providers
{
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
        /// The Serilog ILog implementation to use
        /// </summary>
        public Setting<ILogger> Logger { get; set; }
        /// <summary>
        /// The Serilog Log Level to use
        /// </summary>
        public Setting<LogLevel?> LogLevel { get; set; }
        /// <summary>
        /// A function that given an audit event and an event id, returns the message to log
        /// </summary>
        public Func<AuditEvent, object, object> LogMessageBuilder { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogDataProvider" /> class.
        /// </summary>
        public SerilogDataProvider()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogDataProvider" /> class.
        /// </summary>
        /// <param name="config">Configurator instance.</param>
        public SerilogDataProvider(Action<ISerilogConfigurator> config)
        {
            if (config == null)
            {
                return;
            }
            var logConfig = new SerilogConfigurator();
            config.Invoke(logConfig);
            Logger = logConfig._logger;
            LogLevel = logConfig._logLevel;
            LogMessageBuilder = logConfig._messageBuilder;
        }

        /// <summary>
        /// Stores an event via Serilog.
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
        /// Stores an event related to a previous event, via Serilog.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="auditEvent">The audit event.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            this.Log(auditEvent, eventId);
        }

        private ILogger GetLogger(AuditEvent auditEvent)
        {
            return this.Logger.GetValue(auditEvent) ?? global::Serilog.Log.ForContext(auditEvent.GetType());
        }

        private LogLevel GetLogLevel(AuditEvent auditEvent)
        {
            return this.LogLevel.GetValue(auditEvent) ??
                   (auditEvent.Environment?.Exception != null ? Serilog.LogLevel.Error : Serilog.LogLevel.Info);
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
                case Serilog.LogLevel.Debug:
                    logger.Debug("{Value}", value);
                    break;
                case Serilog.LogLevel.Warn:
                    logger.Warning("{Value}", value);
                    break;
                case Serilog.LogLevel.Error:
                    logger.Error("{Value}", value);
                    break;
                case Serilog.LogLevel.Fatal:
                    logger.Fatal("{Value}", value);
                    break;
                default:
                    logger.Information("{Value}", value);
                    break;
            }
        }
    }
}