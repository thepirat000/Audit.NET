namespace Audit.SignalR
{
    /// <summary>
    /// The SignalR Event Type
    /// </summary>
    public enum SignalrEventType
    {
        /// <summary>Client Connects</summary>
        Connect = 0,
        /// <summary>Client Disconnects</summary>
        Disconnect = 1,
        /// <summary>Client invokes server-side method</summary>
        Incoming = 3
#if ASP_NET
        ,
        /// <summary>Client Reconnects</summary>
        Reconnect = 2,
        /// <summary>Server invokes client-side method</summary>
        Outgoing = 4,
        /// <summary>An error has occurred</summary>
        Error = 5
#endif
    }
}