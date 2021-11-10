using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents an Incoming Error SignalR event
    /// </summary>
    public class SignalrEventError : SignalrEventBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Error; 
        public string HubName { get; set; }
        public string HubType { get; set; }
        public string Exception { get; set; }
        public string ConnectionId { get; set; }
        public string MethodName { get; set; }
        public List<object> Args { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> QueryString { get; set; }
        public string LocalPath { get; set; }
        public string IdentityName { get; set; }

        [JsonIgnore]
        public ExceptionContext ExceptionContext { get; set; }
        [JsonIgnore]
        public IHubIncomingInvokerContext InvokerContext { get; set; }
    }
}