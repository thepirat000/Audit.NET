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
        [JsonConverter(typeof(StringEnumConverter))]
        public override SignalrEventType EventType => SignalrEventType.Incoming; 
        public string HubName { get; set; }
        public string HubType { get; set; }
        public string ConnectionId { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> QueryString { get; set; }
        public string LocalPath { get; set; }
        public string IdentityName { get; set; }
        public string MethodName { get; set; }
        public List<object> Args { get; set; }
        public object Result { get; set; }
        [JsonIgnore]
        public IHubIncomingInvokerContext InvokerContext { get; set; }
    }
}