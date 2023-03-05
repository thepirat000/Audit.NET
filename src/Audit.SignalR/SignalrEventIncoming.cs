using System;
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
    /// Represents an Incoming SignalR event (client invoking server-side method)
    /// </summary>
    public class SignalrEventIncoming : SignalrEventBase
    {
#if ASP_NET
        [JsonConverter(typeof(StringEnumConverter))]
#else
        [JsonConverter(typeof(JsonStringEnumConverter))]
#endif
        public override SignalrEventType EventType => SignalrEventType.Incoming;

        public string HubName { get; set; }
        public string MethodName { get; set; }
        public List<object> Args { get; set; }
        public object Result { get; set; }
#if ASP_NET_CORE
        public string Exception { get; set; }
        public string MethodSignature { get; set; }
#else
        public string LocalPath { get; set; }
#endif
        public string HubType { get; set; }
        public string ConnectionId { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> QueryString { get; set; }
        public string IdentityName { get; set; }
#if ASP_NET
        [JsonIgnore]
        public IHubIncomingInvokerContext InvokerContext { get; set; }
#endif


    }
}