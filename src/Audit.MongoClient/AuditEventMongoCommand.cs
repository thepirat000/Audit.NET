using Audit.Core;

namespace Audit.MongoClient
{
    /// <summary>
    /// Represents the output of the audit process for a Mongo command event
    /// </summary>
    public class AuditEventMongoCommand : AuditEvent
    {
        /// <summary>
        /// Gets or sets the Mongo Command details.
        /// </summary>
        public MongoCommandEvent Command { get; set; }
    }
}
