using System;

namespace Audit.SignalR.Configuration
{
    /// <summary>
    /// Provides a fluent API to configure the filters on the Audit Module
    /// </summary>
    public interface IAuditHubFilterConfigurator
    {
        /// <summary>
        /// To indicate if all the Connect events must be included (true) or excluded (false). Default is true.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubFilterConfigurator IncludeConnectEvent(bool include = true);
        /// <summary>
        /// Specifies the function that determines whether a Connect event is included on the log or not. Default is NULL, meaning all the Connect events are logged.
        /// </summary>
        /// <param name="predicate">A function that takes a connect event info and return a boolean indicating whether this event should be logged</param>
        IAuditHubFilterConfigurator IncludeConnectEvent(Func<SignalrEventConnect, bool> predicate);
        /// <summary>
        /// To indicate if all the Disconnect events must be included (true) or excluded (false). Default is true.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubFilterConfigurator IncludeDisconnectEvent(bool include = true);
        /// <summary>
        /// Specifies the function that determines whether a Disconnect event is included on the log or not. Default is NULL, meaning all the Disconnect events are logged.
        /// </summary>
        /// <param name="predicate">A function that takes disconnect event info and return a boolean indicating whether this event should be logged</param>
        IAuditHubFilterConfigurator IncludeDisconnectEvent(Func<SignalrEventDisconnect, bool> predicate);
        /// <summary>
        /// To indicate if all the Incoming events must be included (true) or excluded (false). Default is true.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubFilterConfigurator IncludeIncomingEvent(bool include = true);
        /// <summary>
        /// Specifies the function that determines whether a Incoming event is included on the log or not. Default is NULL, meaning all the Incoming events are logged.
        /// </summary>
        /// <param name="predicate">A function that takes incoming event info and return a boolean indicating whether this event should be logged</param>
        IAuditHubFilterConfigurator IncludeIncomingEvent(Func<SignalrEventIncoming, bool> predicate);

#if ASP_NET
        /// <summary>
        /// To indicate if all the Reconnect events must be included (true) or excluded (false). Default is true.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubFilterConfigurator IncludeReconnectEvent(bool include = true);
        /// <summary>
        /// Specifies the function that determines whether a Reconnect event is included on the log or not. Default is NULL, meaning all the Reconnect events are logged.
        /// </summary>
        /// <param name="predicate">A function that takes reconnect event info and return a boolean indicating whether this event should be logged</param>
        IAuditHubFilterConfigurator IncludeReconnectEvent(Func<SignalrEventReconnect, bool> predicate);

        /// <summary>
        /// To indicate if all the Outgoing events must be included. Default is true.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubFilterConfigurator IncludeOutgoingEvent(bool include = true);
        /// <summary>
        /// Specifies the function that determines whether a Outgoing event is included on the log or not. Default is NULL, meaning all the Outgoing events are logged.
        /// </summary>
        /// <param name="predicate">A function that takes outgoing event info and return a boolean indicating whether this event should be logged</param>
        IAuditHubFilterConfigurator IncludeOutgoingEvent(Func<SignalrEventOutgoing, bool> predicate);
        /// <summary>
        /// To indicate if all the Error events must be included (true) or excluded (false). Default is true.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubFilterConfigurator IncludeErrorEvent(bool include = true);
        /// <summary>
        /// Specifies the function that determines whether a Error event is included on the log or not. Default is NULL, meaning all the Error events are logged.
        /// </summary>
        /// <param name="predicate">A function that takes error event info and return a boolean indicating whether this event should be logged</param>
        IAuditHubFilterConfigurator IncludeErrorEvent(Func<SignalrEventError, bool> predicate);
#endif
    }
}