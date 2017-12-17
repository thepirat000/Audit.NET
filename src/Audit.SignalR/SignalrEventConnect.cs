using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents a Connect SignalR event
    /// </summary>
    public class SignalrEventConnect : SignalrEventBase
    {
        [JsonProperty(Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Connect;
        [JsonProperty(Order = 8)]
        public string ConnectionId { get; set; }
        [JsonProperty(Order = 10, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Headers { get; set; }
        [JsonProperty(Order = 20, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> QueryString { get; set; }
        [JsonProperty(Order = 30, NullValueHandling = NullValueHandling.Ignore)]
        public string LocalPath { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public string IdentityName { get; set; }
        [JsonIgnore]
        public IHub HubReference { get; set; }
    }
}