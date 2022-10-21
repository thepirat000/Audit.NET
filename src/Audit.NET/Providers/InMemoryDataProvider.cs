using System.Collections.Generic;
using System.Linq;

namespace Audit.Core.Providers
{
    /// <summary>
    /// Data provider to store the audit events in memory.
    /// Use methods GetEvent / GetAllEvents / GetAllEventsOfType to retrieve the events.
    /// Useful for testing purposes.
    /// </summary>
    public class InMemoryDataProvider : AuditDataProvider
    {
        private readonly List<AuditEvent> _events = new List<AuditEvent>();
        private readonly object _lock = new object();

        public override object InsertEvent(AuditEvent auditEvent)
        {
            lock (_lock)
            {
                _events.Add(auditEvent);
                int index = _events.Count - 1;
                return index;
            }
        }
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            int index = (int)eventId;
            lock (_lock)
            {
                _events[index] = auditEvent;
            }
        }
        public override T GetEvent<T>(object eventId)
        {
            int index = (int)eventId;
            lock (_lock)
            {
                return _events[index] as T;
            }
        }

        /// <summary>
        /// Returns a read-only collection of audit events currently stored in memory.
        /// </summary>
        public IList<AuditEvent> GetAllEvents()
        {
            lock (_lock)
            {
                return _events.AsReadOnly();
            }
        }

        /// <summary>
        /// Returns a read-only collection of audit events currently stored in memory, filtered by the given audit event type.
        /// </summary>
        public IList<T> GetAllEventsOfType<T>()
            where T : AuditEvent
        {
            lock (_lock)
            {
                return _events.OfType<T>().ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Removes all audit events currently in memory.
        /// </summary>
        public void ClearEvents()
        {
            lock (_lock)
            {
                _events.Clear();
            }
        }
    }
}
