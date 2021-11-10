using System.Collections.Generic;

namespace Audit.WCF
{
    /// <summary>
    /// The audit event portion with WCF call data
    /// </summary>
    public class WcfEvent
    {
        public string ContractName { get; set; }
        public string OperationName { get; set; }
        public string InstanceQualifiedName { get; set; }
        public bool IsAsync { get; set; }
        public string MethodSignature { get; set; }
        public string Action { get; set; }
        public string ReplyAction { get; set; }
        public string IdentityName { get; set; }
        public string ClientAddress { get; set; }
        public string HostAddress { get; set; }
        public List<AuditWcfEventElement> InputParameters { get; set; }
        public bool Success { get; set; }
        public AuditWcfEventFault Fault { get; set; }
        public AuditWcfEventElement Result { get; set; }
        public List<AuditWcfEventElement> OutputParameters { get; set; }
    }
}
