using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents an Incoming SignalR event (client invoking server-side method)
    /// </summary>
    public class SignalrEventIncoming : SignalrEventBase
    {
        [JsonProperty(Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Incoming; 
        [JsonProperty(Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public string HubName { get; set; }
        [JsonProperty(Order = 7, NullValueHandling = NullValueHandling.Ignore)]
        public string HubType { get; set; }
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
        [JsonProperty(Order = 50)]
        public string MethodName { get; set; }
        [JsonProperty(Order = 60)]
        public List<object> Args { get; set; }
        [JsonProperty(Order = 70)]
        public object Result { get; set; }
        [JsonIgnore]
        public IHubIncomingInvokerContext InvokerContext { get; set; }
    }
}