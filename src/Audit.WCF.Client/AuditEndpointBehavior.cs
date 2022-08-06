using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

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
            clientRuntime.ClientMessageInspectors.Add(new AuditMessageInspector(EventType, IncludeRequestHeaders, IncludeResponseHeaders));
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }
        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
