using Audit.Core;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Represents the output of the audit process for the Audit.EntityFramework
    /// </summary>
    public class AuditEventEntityFramework : AuditEvent
    {
        /// <summary>
        /// Gets or sets the entity framework event details.
        /// </summary>
        public EntityFrameworkEvent EntityFrameworkEvent { get; set; }
    }
}
