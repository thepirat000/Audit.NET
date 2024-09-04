using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Audit.Core;

namespace Audit.Wcf.Client
{
    public class AuditEndpointBehavior : IEndpointBehavior
    {
        /// <summary>
        /// The event type format string, default is "{action}"
        /// Can contain the following placeholder:
        /// - {action}: Replaced by the request action URL
        /// </summary>
        public string EventType { get; set; }
        /// <summary>
        /// Value indicating whether to include the request headers, default is false
        /// </summary>
        public bool IncludeRequestHeaders { get; set; }
        /// <summary>
        /// Value indicating whether to include the response headers, default is false
        /// </summary>
        public bool IncludeResponseHeaders { get; set; }

        /// <summary>
        /// The factory to create the audit scopes. Default is NULL to use the globally configured AuditScopeFactory.
        /// </summary>
        public IAuditScopeFactory AuditScopeFactory { get; set; }

        /// <summary>
        /// The data provider to use. Default is NULL to use the globally configured AuditDataProvider.
        /// </summary>
        public AuditDataProvider AuditDataProvider { get; set; }

        public AuditEndpointBehavior()
        {
        }

        public AuditEndpointBehavior(string eventType, bool includeRequestHeaders, bool includeResponseHeaders)
        {
            IncludeRequestHeaders = includeRequestHeaders;
            IncludeResponseHeaders = includeResponseHeaders;
            EventType = eventType;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new AuditMessageInspector(EventType, IncludeRequestHeaders, IncludeResponseHeaders, AuditScopeFactory, AuditDataProvider));
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }
        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
