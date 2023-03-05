using System;
using Audit.Core;
using Audit.SignalR.Configuration;
#if ASP_NET
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
#else
using Microsoft.AspNetCore.SignalR;
#endif

namespace Audit.SignalR
{
    public static class SignalrExtensions
    {
        /// <summary>
        /// Gets the SignalR Event portion of an Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static SignalrEventBase GetSignalrEvent(this AuditEvent auditEvent)
        {
            return (auditEvent as AuditEventSignalr)?.Event;
        }

        /// <summary>
        /// Gets the SignalR Event portion of an Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static T GetSignalrEvent<T>(this AuditEvent auditEvent)
            where T : SignalrEventBase
        {
            return (auditEvent as AuditEventSignalr)?.Event as T;
        }

#if ASP_NET
        /// <summary>
        /// Adds a custom field to the current Incoming event
        /// </summary>
        /// <typeparam name="TC">The type of the value.</typeparam>
        /// <param name="hub">The hub object.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value object.</param>
        /// <param name="serialize">if set to <c>true</c> the field is serialized immediately.</param>
        public static void AddCustomField<TC>(this IHub hub, string fieldName, TC value, bool serialize = false)
        {
            var scope = hub.GetIncomingAuditScope();
            scope?.SetCustomField(fieldName, value, serialize);
        }

        /// <summary>
        /// Gets the current AuditScope related to an Incoming message. 
        /// This can be used on the Hub methods to get the current scope with the Incoming event information.
        /// </summary>
        public static AuditScope GetIncomingAuditScope(this IHub hub)
        {
            if (hub.Context.Request.Environment.TryGetValue(AuditPipelineModule.AuditScopeIncomingEnvironmentKey, out var scope))
            {
                return scope as AuditScope;
            }
            return null;
        }

        /// <summary>
        /// Gets the current AuditScope related to a Connect message. 
        /// This can be used on the Hub's OnConnected method to get the audit scope containing the Connect message event
        /// </summary>
        public static AuditScope GetConnectAuditScope(this IHub hub)
        {
            if (hub.Context.Request.Environment.TryGetValue(AuditPipelineModule.AuditScopeConnectEnvironmentKey, out var scope))
            {
                return scope as AuditScope;
            }
            return null;
        }

        /// <summary>
        /// Gets the current AuditScope related to a Disconnect message. 
        /// This can be used on the Hub's OnDisconnected method to get the audit scope containing the Disconnect message event
        /// </summary>
        public static AuditScope GetDisconnectAuditScope(this IHub hub)
        {
            if (hub.Context.Request.Environment.TryGetValue(AuditPipelineModule.AuditScopeDisconnectEnvironmentKey, out var scope))
            {
                return scope as AuditScope;
            }
            return null;
        }

        /// <summary>
        /// Gets the current AuditScope related to a Reconnect message. 
        /// This can be used on the Hub's OnReconnected method to get the audit scope containing the Reconnect message event
        /// </summary>
        public static AuditScope GetReconnectAuditScope(this IHub hub)
        {
            if (hub.Context.Request.Environment.TryGetValue(AuditPipelineModule.AuditScopeReconnectEnvironmentKey, out var scope))
            {
                return scope as AuditScope;
            }
            return null;
        }

        /// <summary>
        /// Adds an Audit Module to the hub pipeline. This must be called the before any methods on the Microsoft.AspNet.SignalR.Hubs.IHubPipelineInvoker are invoked.
        /// </summary>
        /// <param name="pipeline">The hub pipeline</param>
        /// <param name="config">The audit module configuration</param>
        /// <returns></returns>
        public static IHubPipeline AddAuditModule(this IHubPipeline pipeline, Action<IAuditHubConfigurator> config)
        {
            GlobalHost.HubPipeline.AddModule(AuditPipelineModule.Create(config));
            return pipeline;
        }
#else
        /// <summary>
        /// Adds a custom field to the current Incoming event
        /// </summary>
        /// <typeparam name="TC">The type of the value.</typeparam>
        /// <param name="hub">The hub object.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value object.</param>
        /// <param name="serialize">if set to <c>true</c> the field is serialized immediately.</param>
        public static void AddCustomField<TC>(this Hub hub, string fieldName, TC value, bool serialize = false)
        {
            hub.GetAuditScope()?.SetCustomField(fieldName, value, serialize);
        }

        /// <summary>
        /// Gets the current AuditScope related to an Incoming message. 
        /// This can be used on the Hub methods to get the current scope with the Incoming event information.
        /// </summary>
        public static AuditScope GetAuditScope(this Hub hub)
        {
            if (hub.Context.GetHttpContext()?.Items.TryGetValue(AuditHubFilter.AuditScopeKey, out var scope) == true)
            {
                return scope as AuditScope;
            }
            return null;
        }

        /// <summary>
        /// Adds the hub filter to the pipeline.
        /// </summary>
        /// <param name="options">The audit configuration options</param>
        /// <param name="config">The audit filter configuration</param>
        public static void AddAuditFilter(this HubOptions options, Action<IAuditHubConfigurator> config)
        {
            options.AddFilter(new AuditHubFilter(config));
        }
#endif
    }
}