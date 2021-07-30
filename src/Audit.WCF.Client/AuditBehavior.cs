using Audit.Core;
using System;
using System.Configuration;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel.Configuration;

namespace Audit.Wcf.Client
{
    public class AuditBehavior : BehaviorExtensionElement
    {
        /* // TODO: FDCG: 
        /// <summary>
        /// Gets the current audit scope for the running thread.
        /// Get this property from an audited WCF method to get the current audit scope.
        /// </summary>
        public static IAuditScope CurrentAuditScope
        {
            get
            {
                return CallContext.LogicalGetData(WcfContextScopeKey) as IAuditScope;
            }
            internal set
            {
                CallContext.LogicalSetData(WcfContextScopeKey, value);
            }
        }*/

        /// <summary>
        /// Gets or sets the event type.
        /// Can contain the following placeholders:
        /// - {action}: Replaced with the action name
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
        /// Gets or sets a value that indicates whether the output should include the request headers
        /// </summary>
        [ConfigurationProperty("includeRequestHeaders", IsRequired = false)]
        public virtual bool? IncludeRequestHeaders
        {
            get
            {
                return this["includeRequestHeaders"] as bool?;
            }
            set
            {
                this["includeRequestHeaders"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the output should include the request headers
        /// </summary>
        [ConfigurationProperty("includeResponseHeaders", IsRequired = false)]
        public virtual bool? IncludeResponseHeaders
        {
            get
            {
                return this["includeResponseHeaders"] as bool?;
            }
            set
            {
                this["includeResponseHeaders"] = value;
            }
        }

        public override Type BehaviorType
        {
            get { return typeof(AuditEndpointBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new AuditEndpointBehavior(EventType, IncludeRequestHeaders == true, IncludeResponseHeaders == true);
        }
    }
}
