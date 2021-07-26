using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents an Outgoing SignalR event (server invoking client-side method)
    /// </summary>
    public class SignalrEventOutgoing : SignalrEventBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Outgoing;
        public string HubName { get; set; }
        public string Signal { get; set; }
        public List<string> Signals { get; set; }
        public string MethodName { get; set; }
        public List<object> Args { get; set; }
        [JsonIgnore]
        public IHubOutgoingInvokerContext InvokerContext { get; set; }
    }
}