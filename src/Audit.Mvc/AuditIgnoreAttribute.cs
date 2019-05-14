using System;

namespace Audit.Mvc
{
    /// <summary>
    /// Use to selectively ignore controllers, action methods or method parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class AuditIgnoreAttribute : Attribute
    {
    }
}
