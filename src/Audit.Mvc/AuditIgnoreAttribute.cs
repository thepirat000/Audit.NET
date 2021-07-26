using System;

namespace Audit.Mvc
{
    /// <summary>
    /// Use to selectively ignore controllers, action methods or method parameters
    /// </summary>
#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451 || NET5_0
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = true, AllowMultiple = false)]
#else
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
#endif
    public sealed class AuditIgnoreAttribute : Attribute
    {
    }
}
