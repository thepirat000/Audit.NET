using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Use to avoid logging specific operations, parameters or return values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = true, AllowMultiple = false)]
    public sealed class AuditIgnoreAttribute : Attribute
    {
    }
}
