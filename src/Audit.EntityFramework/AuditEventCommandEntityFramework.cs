#if EF_CORE_5_OR_GREATER
using Audit.Core;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Represents the output of the audit process for the Low-Level Commands Interceptor
    /// </summary>
    public class AuditEventCommandEntityFramework : AuditEvent
    {
        public CommandEvent CommandEvent { get; set; }
    }
}
#endif