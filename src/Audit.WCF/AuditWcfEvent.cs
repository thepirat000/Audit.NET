using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.WCF
{
    /// <summary>
    /// The audit event portion with WCF call data
    /// </summary>
    public class AuditWcfEvent
    {
        [JsonProperty(Order = 10)]
        public string ContractName { get; set; }
        [JsonProperty(Order = 20)]
        public string OperationName { get; set; }
        [JsonProperty(Order = 30)]
        public string InstanceQualifiedName { get; set; }
        [JsonProperty(Order = 40)]
        public string MethodSignature { get; set; }
        [JsonProperty(Order = 50)]
        public string Action { get; set; }
        [JsonProperty(Order = 60)]
        public string ReplyAction { get; set; }
        [JsonProperty(Order = 70, NullValueHandling = NullValueHandling.Ignore)]
        public string IdentityName { get; set; }
        [JsonProperty(Order = 80)]
        public string ClientAddress { get; set; }
        [JsonProperty(Order = 90)]
        public string HostAddress { get; set; }
        [JsonProperty(Order = 100)]
        public bool Success { get; set; }
        [JsonProperty(Order = 110, NullValueHandling = NullValueHandling.Ignore)]
        public AuditWcfEventFault Fault { get; set; }
        [JsonProperty(Order = 120)]
        public AuditWcfEventElement Result { get; set; }
        [JsonProperty(Order = 130)]
        public List<AuditWcfEventElement> InputParameters { get; set; }
        [JsonProperty(Order = 140, NullValueHandling = NullValueHandling.Ignore)]
        public List<AuditWcfEventElement> OutputParameters { get; set; }
    }
}
