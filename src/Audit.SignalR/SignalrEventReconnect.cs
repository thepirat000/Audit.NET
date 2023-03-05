#if ASP_NET
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Audit.SignalR
{
    /// <summary>
    /// Represents a Reconnect SignalR event
    /// </summary>
    public class SignalrEventReconnect : SignalrEventBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Reconnect;
        public string ConnectionId { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> QueryString { get; set; }
        public string LocalPath { get; set; }
        public string IdentityName { get; set; }
        [JsonIgnore]
        public IHub HubReference { get; set; }
    }
}
#endif