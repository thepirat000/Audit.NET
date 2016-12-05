using Audit.Core;
using Newtonsoft.Json;

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
        [JsonProperty(Order = 10)]
        public EntityFrameworkEvent EntityFrameworkEvent { get; set; }
    }
}
