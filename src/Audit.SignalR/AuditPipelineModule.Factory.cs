﻿#if ASP_NET
using System;
using Audit.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hubs;

namespace Audit.SignalR
{
    public partial class AuditPipelineModule : HubPipelineModule
    {
        /// <summary>
        /// Creates a new AuditPipelineModule using the fluent configuration API.
        /// </summary>
        /// <param name="builder">Module configuration as a fluent API</param>
        public static AuditPipelineModule Create(Action<IAuditHubConfigurator> builder)
        {
            var config = new AuditHubConfigurator();
            builder.Invoke(config);
            var filters = config._filters as AuditHubFilterConfigurator ?? new AuditHubFilterConfigurator();
            var module = new AuditPipelineModule()
            {
                AuditDataProvider = config._dataProvider,
                AuditDisabled = config._auditDisabled,
                CreationPolicy = config._creationPolicy,
                AuditEventType = config._eventType,
                IncludeHeaders = config._includeHeaders,
                IncludeQueryString = config._includeQueryString,
                ConnectEventsFilter = filters._connectEventsFilter,
                ReconnectEventsFilter = filters._reconnectEventsFilter,
                DisconnectEventsFilter = filters._disconnectEventsFilter,
                ErrorEventsFilter = filters._errorEventsFilter,
                IncomingEventsFilter = filters._incomingEventsFilter,
                OutgoingEventsFilter = filters._outgoingEventsFilter
            };
            return module;
        }
    }
}
#endif