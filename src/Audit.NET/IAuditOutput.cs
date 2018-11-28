using System.Collections.Generic;
using Newtonsoft.Json;

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
        [JsonExtensionData]
        Dictionary<string, object> CustomFields { get; set; }
        /// <summary>
        /// Serialize to JSON string
        /// </summary>
        /// <returns></returns>
        string ToJson();
    }
}
