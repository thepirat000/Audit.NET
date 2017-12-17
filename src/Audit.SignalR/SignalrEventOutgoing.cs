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
        [JsonProperty(Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Outgoing;
        [JsonProperty(Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public string HubName { get; set; }
        [JsonProperty(Order = 10, NullValueHandling = NullValueHandling.Ignore)]
        public string Signal { get; set; }
        [JsonProperty(Order = 15, NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Signals { get; set; }
        [JsonProperty(Order = 20)]
        public string MethodName { get; set; }
        [JsonProperty(Order = 30)]
        public List<object> Args { get; set; }
        [JsonIgnore]
        public IHubOutgoingInvokerContext InvokerContext { get; set; }
    }
}