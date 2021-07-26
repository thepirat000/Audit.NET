namespace Audit.SignalR
{
    /// <summary>
    /// The base type for all the SignalR events representations
    /// </summary>
    public class SignalrEventBase
    {
        public virtual SignalrEventType EventType { get; set; }
    }
}