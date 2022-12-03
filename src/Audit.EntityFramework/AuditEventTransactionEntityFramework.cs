#if EF_CORE_5_OR_GREATER
using Audit.Core;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Represents the output of the audit process for the Low-Level Transactions Interceptor
    /// </summary>
    public class AuditEventTransactionEntityFramework : AuditEvent
    {
        public TransactionEvent TransactionEvent { get; set; }
    }
}
#endif