#if EVENTLOG_CORE || NET462 || NET472 
using System;
using System.Diagnostics;
using Audit.Core.ConfigurationApi;

namespace Audit.Core.Providers
{
    /// <summary>
    /// Writes to the windows event log
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - SourcePath: Event Source (default: Application)
    /// - LogName: Log name (default: Application)
    /// - MachineName: Event Source (default: .)
    /// </remarks>
    public class EventLogDataProvider : AuditDataProvider
    {
        /// <summary>
        /// The EventLog Log Name
        /// </summary>
        public Setting<string> LogName { get; set; } = "Application";

        /// <summary>
        /// The EventLog Source Path
        /// </summary>
        public Setting<string> SourcePath { get; set; } = "Application";

        /// <summary>
        /// The Message builder. A function to obtain the message string to log to the event log.
        /// </summary>
        public Func<AuditEvent, string> MessageBuilder { get; set; }

        /// <summary>
        /// The Machine name (use "." to set local machine)
        /// </summary>
        public Setting<string> MachineName { get; set; } = ".";

        public EventLogDataProvider()
        {
        }

        public EventLogDataProvider(Action<IEventLogProviderConfigurator> config)
        {
            var eventLogConfig = new EventLogProviderConfigurator();
            if (config != null)
            {
                config.Invoke(eventLogConfig);
                LogName = eventLogConfig._logName;
                SourcePath = eventLogConfig._sourcePath;
                MachineName = eventLogConfig._machineName;
                MessageBuilder = eventLogConfig._messageBuilder;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var source = SourcePath.GetValue(auditEvent);
            var logName = LogName.GetValue(auditEvent);
            var message = MessageBuilder != null ? MessageBuilder.Invoke(auditEvent) : auditEvent.ToJson();
            var machine = MachineName.GetValue(auditEvent);
            
            if (!EventLog.SourceExists(source, machine))
            {
                EventLog.CreateEventSource(source, logName);
            }

            using (var eventLog = new EventLog(logName, machine, source))
            {
                eventLog.WriteEntry(message, auditEvent.Environment.Exception == null ? EventLogEntryType.Information : EventLogEntryType.Error);
            }

            return null;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            InsertEvent(auditEvent);
        }
    }
}
#endif
