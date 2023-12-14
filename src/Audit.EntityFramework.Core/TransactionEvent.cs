#if EF_CORE_5_OR_GREATER
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        internal DbContext DbContext { get; set; }

        /// <summary>
        /// Returns the DbContext associated to this event
        /// </summary>
        public DbContext GetDbContext()
        {
            return DbContext;
        }
    }
}
#endif