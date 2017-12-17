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
        [JsonProperty(Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Error; 
        [JsonProperty(Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public string HubName { get; set; }
        [JsonProperty(Order = 7, NullValueHandling = NullValueHandling.Ignore)]
        public string HubType { get; set; }
        [JsonProperty(Order = 10)]
        public string Exception { get; set; }
        [JsonProperty(Order = 15)]
        public string ConnectionId { get; set; }
        [JsonProperty(Order = 20)]
        public string MethodName { get; set; }
        [JsonProperty(Order = 25)]
        public List<object> Args { get; set; }
        [JsonProperty(Order = 30, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Headers { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> QueryString { get; set; }
        [JsonProperty(Order = 50, NullValueHandling = NullValueHandling.Ignore)]
        public string LocalPath { get; set; }
        [JsonProperty(Order = 60, NullValueHandling = NullValueHandling.Ignore)]
        public string IdentityName { get; set; }

        [JsonIgnore]
        public ExceptionContext ExceptionContext { get; set; }
        [JsonIgnore]
        public IHubIncomingInvokerContext InvokerContext { get; set; }
    }
}