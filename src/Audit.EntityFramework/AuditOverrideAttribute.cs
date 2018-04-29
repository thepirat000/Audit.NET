using System;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Used in OptOut mode to ignore an entity (class) on the Audit logs. Also can be used to ignore entity properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class AuditOverrideAttribute : Attribute
    {
        public object Value { get; set; }
        public AuditOverrideAttribute()
        {
        }
        public AuditOverrideAttribute(object value)
        {
            Value = value;
        }
    }
}