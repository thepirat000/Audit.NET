using System;
using System.Diagnostics;

namespace Audit.Core.Providers
{
#if NET45 || NETSTANDARD2_0
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
        public string LogName { get; set; } = "Application";

        /// <summary>
        /// The EventLog Source Path
        /// </summary>
        public string SourcePath { get; set; } = "Application";

        /// <summary>
        /// The Message builder. A function to obtain the message string to log to the event log.
        /// </summary>
        public Func<AuditEvent, string> MessageBuilder { get; set; }

        /// <summary>
        /// The Machine name (use "." to set local machine)
        /// </summary>
        public string MachineName { get; set; } = ".";

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var source = SourcePath;
            var logName = LogName;
            var message = MessageBuilder != null ? MessageBuilder.Invoke(auditEvent) : auditEvent.ToJson();
            if (!EventLog.SourceExists(source, MachineName))
            {
                EventLog.CreateEventSource(source, logName);
            }
            using (var eventLog = new EventLog(logName, MachineName, source))
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
#endif
}
