using Audit.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.NET.MySql
{
    /// <summary>
    /// Represents a custom column on the audit table
    /// </summary>
    public class CustomColumn
    {
        public string Name { get; set; }
        public Func<AuditEvent, object> Value { get; set; }

        public CustomColumn()
        {
        }

        public CustomColumn(string name, Func<AuditEvent, object> value)
        {
            Name = name;
            Value = value;
        }
    }
}
