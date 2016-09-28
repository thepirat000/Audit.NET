using Audit.Core.Extensions;
using Newtonsoft.Json;

namespace Audit.WCF
{
    /// <summary>
    /// An element type/value object
    /// </summary>
    public class AuditWcfEventElement
    {
        [JsonProperty(Order = 10)]
        public string Type { get; set; }
        [JsonProperty(Order = 20)]
        public object Value { get; set; }

        public AuditWcfEventElement(object value)
        {
            Type = value?.GetType().GetFullTypeName();
            Value = value;
        }
    }
}