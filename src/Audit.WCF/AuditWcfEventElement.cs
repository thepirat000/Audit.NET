using Audit.Core.Extensions;

namespace Audit.WCF
{
    /// <summary>
    /// An element type/value object
    /// </summary>
    public class AuditWcfEventElement
    {
        public string Type { get; set; }
        public object Value { get; set; }

        public AuditWcfEventElement(object value)
        {
            Type = value?.GetType().GetFullTypeName();
            Value = value;
        }
    }
}