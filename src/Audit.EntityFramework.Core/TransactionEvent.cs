#if EF_CORE_3_OR_GREATER
using System.Data;

namespace Audit.EntityFramework
{

    /// <summary>
    /// Event information for transaction interception
    /// </summary>
    public class TransactionEvent : InterceptorEventBase
    {
        public string EventIdCode { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// The transaction action. One of: "Start", "Commit" or "Rollback"
        /// </summary>
        public string Action { get; set; }
    }
}
#endif