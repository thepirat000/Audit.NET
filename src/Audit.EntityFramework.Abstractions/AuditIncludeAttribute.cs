using System;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Used with OptIn Mode to include the entity or property on the Audit logs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class AuditIncludeAttribute : Attribute
    {

    }
}