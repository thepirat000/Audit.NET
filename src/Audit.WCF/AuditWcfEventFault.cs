namespace Audit.WCF
{
    /// <summary>
    /// Descripbes a fault in a WCF method call
    /// </summary>
    public class AuditWcfEventFault
    {
        public string FaultType { get; set; }
        public string Exception { get; set; }
        public string FaultCode { get; set; }
        public string FaultAction { get; set; }
        public string FaultReason { get; set; }
        public AuditWcfEventElement FaultDetail { get; set; }
    }
}