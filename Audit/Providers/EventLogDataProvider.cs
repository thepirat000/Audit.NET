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
    /// </remarks>
    public class EventLogDataProvider : AuditDataProvider
    {
        private string _sourcePath = "Application";
        private string _machineName = ".";
        private string _logName = "Application";

        public string LogName
        {
            get { return _logName; }
            set { _logName = value; }
        }

        public string SourcePath
        {
            get { return _sourcePath; }
            set { _sourcePath = value; }
        }

        public string MachineName
        {
            get { return _machineName; }
            set { _machineName = value; }
        }

        public override void WriteEvent(AuditEvent auditEvent)
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
        }

    }
}
