#if ASP_NET_CORE
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
#endif

namespace Audit.SignalR
{
    /// <summary>
    /// The base type for all the SignalR events representations
    /// </summary>
#if ASP_NET_CORE
    [JsonDerivedType(typeof(SignalrEventIncoming))]
    [JsonDerivedType(typeof(SignalrEventConnect))]
    [JsonDerivedType(typeof(SignalrEventDisconnect))]
#endif
    public class SignalrEventBase
    {
        public virtual SignalrEventType EventType { get; set; }

#if ASP_NET_CORE
        [JsonIgnore]
        internal Hub Hub { get; set; }

        /// <summary>
        /// Returns the SignalR Hub associated to this event
        /// </summary>
        public Hub GetHub()
        {
            return Hub;
        }
#endif
    }
}