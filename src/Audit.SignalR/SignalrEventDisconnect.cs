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
    /// Represents a Disconnect SignalR event
    /// </summary>
    public class SignalrEventDisconnect : SignalrEventBase
    {
#if ASP_NET
        [JsonConverter(typeof(StringEnumConverter))]
#else
        [JsonConverter(typeof(JsonStringEnumConverter))]
#endif
        public override SignalrEventType EventType => SignalrEventType.Disconnect;

#if ASP_NET
        public string LocalPath { get; set; }
        public bool StopCalled { get; set; }
#endif

        public string ConnectionId { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> QueryString { get; set; }
        public string IdentityName { get; set; }

#if ASP_NET
        [JsonIgnore]
        public IHub HubReference { get; set; }
#else
        public string Exception { get; set; }
#endif
    }
}