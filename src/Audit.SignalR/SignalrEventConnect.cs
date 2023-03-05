using System.Collections.Generic;
#if ASP_NET
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace Audit.SignalR
{
    /// <summary>
    /// Represents a Connect SignalR event
    /// </summary>
    public class SignalrEventConnect : SignalrEventBase
    {
#if ASP_NET
        [JsonConverter(typeof(StringEnumConverter))]
#else
        [JsonConverter(typeof(JsonStringEnumConverter))]
#endif
        public override SignalrEventType EventType => SignalrEventType.Connect;

#if ASP_NET
        public string LocalPath { get; set; }
#endif

        public string ConnectionId { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> QueryString { get; set; }

        public string IdentityName { get; set; }

#if ASP_NET
        [JsonIgnore]
        public IHub HubReference { get; set; }
#endif
    }
}