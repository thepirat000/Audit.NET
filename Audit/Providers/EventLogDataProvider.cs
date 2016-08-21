using System.Diagnostics;

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
        private string _sourcePath = "Application";
        private string _machineName = ".";
        private string _logName = "Application";

        /// <summary>
        /// The EventLog Log Name
        /// </summary>
        public string LogName
        {
            get { return _logName; }
            set { _logName = value; }
        }

        /// <summary>
        /// The EventLog Source Path
        /// </summary>
        public string SourcePath
        {
            get { return _sourcePath; }
            set { _sourcePath = value; }
        }

        /// <summary>
        /// The Machine name (use "." to set local machine)
        /// </summary>
        public string MachineName
        {
            get { return _machineName; }
            set { _machineName = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var source = _sourcePath;
            var logName = LogName;
            var json = auditEvent.ToJson();
            if (!EventLog.SourceExists(source, _machineName))
            {
                EventLog.CreateEventSource(source, logName);
            }
            using (var eventLog = new EventLog(logName, _machineName, source))
            {
                eventLog.WriteEntry(json, auditEvent.Environment.Exception == null ? EventLogEntryType.Information : EventLogEntryType.Error);
            }
            return null;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            InsertEvent(auditEvent);
        }
    }
}
