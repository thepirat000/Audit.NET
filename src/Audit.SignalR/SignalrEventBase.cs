using Newtonsoft.Json;

namespace Audit.SignalR
{
    /// <summary>
    /// The base type for all the SignalR events representations
    /// </summary>
    public class SignalrEventBase
    {
        [JsonProperty(Order = 5)]
        public virtual SignalrEventType EventType { get; set; }
    }
}