using System.Collections.Generic;

namespace Audit.Core
{
    /// <summary>
    /// Common interface for objects intended to be output of the audit events
    /// </summary>
    public interface IAuditOutput
    {
        /// <summary>
        /// Extension fields
        /// </summary>
        Dictionary<string, object> CustomFields { get; set; }
        /// <summary>
        /// Serialize to JSON string
        /// </summary>
        /// <returns></returns>
        string ToJson();
    }
}
