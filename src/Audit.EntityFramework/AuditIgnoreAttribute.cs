using System;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Used in OptOut mode to ignore the entity on the Audit logs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AuditIgnoreAttribute : Attribute
    {

    }
}