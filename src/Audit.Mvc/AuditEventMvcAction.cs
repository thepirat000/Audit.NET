using Audit.Core;

namespace Audit.Mvc
{
    /// <summary>
    /// Represents the output of the audit process for an MVC action
    /// </summary>
    public class AuditEventMvcAction : AuditEvent
    {
        /// <summary>
        /// Gets or sets the action details.
        /// </summary>
        /// <value>The action.</value>
        public AuditAction Action { get; set; }
    }
}
