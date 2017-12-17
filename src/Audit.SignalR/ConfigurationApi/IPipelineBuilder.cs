using System;
using Audit.Core;

namespace Audit.SignalR.Configuration
{
    /// <summary>
    /// Provides a fluent API to configure the Audit Module
    /// </summary>
    public interface IPipelineBuilder
    {
        /// <summary>
        /// Sets the event type string to use on the audit event. (Default is the event name). Can contain the following placeholder: 
        /// - {event}: replaced with the SignalR event name (Connect, Reconnect, Disconnect, Incoming, Outgoing, Error).
        /// </summary>
        /// <param name="eventType">The event type string</param>
        IPipelineBuilder EventType(string eventType);
        /// <summary>
        /// Set the event creation policy to use. Default is NULL to use the globally configured creation policy.
        /// </summary>
        /// <param name="policy">The event creation policy to use</param>
        IPipelineBuilder WithCreationPolicy(EventCreationPolicy? policy);
        /// <summary>
        /// Set the Audit Data Provider to use for storing the events. Default is NULL to use the globally configured data provider.
        /// </summary>
        /// <param name="provider">The data provider to use for storing the events</param>
        /// <returns></returns>
        IPipelineBuilder WithDataProvider(AuditDataProvider provider);
        /// <summary>
        /// To indicate if the audit should include the request headers (Valid for events Connect, Reconnect, Disconnect, Incoming and Error). Default is false.
        /// </summary>
        /// <param name="include">The include value</param>
        IPipelineBuilder IncludeHeaders(bool include = true);
        /// <summary>
        /// To indicate if the audit should include the request Query String (Valid for events Connect, Reconnect, Disconnect, Incoming and Error). Default is false.
        /// </summary>
        /// <param name="include">The include value</param>
        /// <returns></returns>
        IPipelineBuilder IncludeQueryString(bool include = true);
        /// <summary>
        /// Disable the audit.
        /// </summary>
        void DisableAudit();
        /// <summary>
        /// Provides a fluent API to configure the event filters.
        /// </summary>
        IPipelineBuilder Filters(Action<IPipelineBuilderFilters> config);
    }
}