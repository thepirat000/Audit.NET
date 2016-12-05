using Audit.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Represents the output of the audit process for the Audit.DynamicProxy
    /// </summary>
    public class AuditEventIntercept : AuditEvent
    {
        /// <summary>
        /// Gets or sets the intercepted event details.
        /// </summary>
        [JsonProperty(Order = 10)]
        public InterceptEvent InterceptEvent { get; set; }
    }
}
