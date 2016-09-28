using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Use to avoid specific operations to be logged.
    /// </summary>
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class AuditIgnoreAttribute : Attribute
    {
    }
}
