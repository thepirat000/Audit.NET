using Audit.Core;
using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace Audit.WCF
{
    /// <summary>
    /// AuditBehavior element for Audit.Wcf behavior configuration.
    /// </summary>
    public class AuditBehavior : BehaviorExtensionElement
    {
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

        /// <summary>
        /// Gets or sets the event type.
        /// Can contain the following placeholders:
        /// - {contract}: Replaced with the contract name (service interface name)
        /// - {operation}: Replaces with the operation name (service method name)
        /// </summary>
        [ConfigurationProperty("eventType", IsRequired = false)]
        public virtual string EventType
        {
            get
            {
                return this["eventType"] as string;
            }
            set
            {
                this["eventType"] = value;
            }
        }

        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        /// <value>The type of the behavior.</value>
        public override Type BehaviorType
        {
            get
            {
                return typeof(AuditBehaviorAttribute);
            }
        }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        /// <returns>The behavior extension.</returns>
        protected override object CreateBehavior()
        {
            return new AuditBehaviorAttribute(EventType);
        }
    }
}