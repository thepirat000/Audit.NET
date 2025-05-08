using System;
using Audit.Core;

namespace Audit.SignalR.Configuration
{
    /// <summary>
    /// Provides a fluent API to configure the Audit Module/Filter
    /// </summary>
    public interface IAuditHubConfigurator
    {
#if ASP_NET
        /// <summary>
        /// Sets the event type string to use on the audit event. (Default is the event name). Can contain the following placeholder:
        /// - {event}: replaced with the SignalR event name (Connect, Reconnect, Disconnect, Incoming, Outgoing, Error).
        /// </summary>
        /// <param name="eventType">The event type string</param>
        IAuditHubConfigurator EventType(string eventType);
#else
        /// <summary>
        /// Sets the event type string to use on the audit event. (Default is the event name). Can contain the following placeholder:
        /// - {event}: replaced with the SignalR event name (Connect, Reconnect, Incoming).
        /// - {hub}: replaced with the hub name.
        /// - {method}: replaced with the hub method name.
        /// </summary>
        /// <param name="eventType">The event type string</param>
        IAuditHubConfigurator EventType(string eventType);
#endif

        /// <summary>
        /// To indicate if the audit should include the request headers (Valid for events Connect, Reconnect, Disconnect, Incoming and Error). Default is false.
        /// </summary>
        /// <param name="include">The include value</param>
        IAuditHubConfigurator IncludeHeaders(bool include = true);
        /// <summary>
        /// To indicate if the audit should include the request Query String (Valid for events Connect, Reconnect, Disconnect, Incoming and Error). Default is false.
        /// </summary>
        /// <param name="include">The include value</param>
        /// <returns></returns>
        IAuditHubConfigurator IncludeQueryString(bool include = true);
        /// <summary>
        /// Disable the audit.
        /// </summary>
        void DisableAudit();
        /// <summary>
        /// Provides a fluent API to configure the event filters.
        /// </summary>
        IAuditHubConfigurator Filters(Action<IAuditHubFilterConfigurator> config);
        /// <summary>
        /// Set the event creation policy to use. Default is NULL to use the globally configured creation policy.
        /// </summary>
        /// <param name="policy">The event creation policy to use</param>
        IAuditHubConfigurator WithCreationPolicy(EventCreationPolicy? policy);
        /// <summary>
        /// Set the Audit Data Provider to use for storing the events. Default is NULL to use the globally configured data provider.
        /// </summary>
        /// <param name="provider">The data provider to use for storing the events</param>
        /// <returns></returns>
        IAuditHubConfigurator WithDataProvider(IAuditDataProvider provider);
    }
}