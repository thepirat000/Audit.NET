using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents a Disconnect SignalR event
    /// </summary>
    public class SignalrEventDisconnect : SignalrEventBase
    {
        [JsonProperty(Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Disconnect;
        [JsonProperty(Order = 10, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool StopCalled { get; set; }
        [JsonProperty(Order = 15)]
        public string ConnectionId { get; set; }
        [JsonProperty(Order = 20, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Headers { get; set; }
        [JsonProperty(Order = 30, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> QueryString { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public string LocalPath { get; set; }
        [JsonProperty(Order = 50, NullValueHandling = NullValueHandling.Ignore)]
        public string IdentityName { get; set; }
        [JsonIgnore]
        public IHub HubReference { get; set; }
    }
}