using Newtonsoft.Json;

namespace Audit.WCF
{
    /// <summary>
    /// Descripbes a fault in a WCF method call
    /// </summary>
    public class AuditWcfEventFault
    {
        [JsonProperty(Order = 10)]
        public string FaultType { get; set; }
        [JsonProperty(Order = 20)]
        public string Exception { get; set; }
        [JsonProperty(Order = 30, NullValueHandling = NullValueHandling.Ignore)]
        public string FaultCode { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public string FaultAction { get; set; }
        [JsonProperty(Order = 50, NullValueHandling = NullValueHandling.Ignore)]
        public string FaultReason { get; set; }
        [JsonProperty(Order = 60, NullValueHandling = NullValueHandling.Ignore)]
        public AuditWcfEventElement FaultDetail { get; set; }
    }
}