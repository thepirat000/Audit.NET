using Audit.Core;

namespace Audit.WCF
{
    /// <summary>
    /// WCF Configuration class
    /// </summary>
    public class Configuration
    {
        internal const string CustomFieldName = "WcfEvent";
        private const string WcfContextScopeKey = "AuditScope";

        /// <summary>
        /// Gets the current audit scope for the running thread.
        /// Get this property from an audited WCF method to get the current audit scope.
        /// </summary>
        public static AuditScope CurrentAuditScope
        {
            get
            {
                object auditScope;
                WcfOperationContext.Current.Items.TryGetValue(WcfContextScopeKey, out auditScope);
                return auditScope as AuditScope;
            }
            internal set
            {
                if (value == null)
                {
                    WcfOperationContext.Current.Items.Remove(WcfContextScopeKey);
                }
                else
                {
                    WcfOperationContext.Current.Items[WcfContextScopeKey] = value;
                }
            }
        }
    }

}
