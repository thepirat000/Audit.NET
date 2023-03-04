using System;
using Audit.Core;

namespace Audit.SqlServer
{
    /// <summary>
    /// Represents a custom column on the audit table
    /// </summary>
    public class CustomColumn
    {
        public string Name { get; set; }
        public Func<AuditEvent, object> Value { get; set; }
        public Func<AuditEvent, bool> Guard { get; set; }

        public CustomColumn()
        {
        }

        public CustomColumn(string name, Func<AuditEvent, object> value)
        {
            Name = name;
            Value = value;
        }

        public CustomColumn(string name, Func<AuditEvent, object> value, Func<AuditEvent, bool> guard)
        {
            Name = name;
            Value = value;
            Guard = guard;
        }
    }
}
