#if ASP_NET_CORE
#nullable enable
using Audit.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Audit.SignalR.Configuration;

namespace Audit.SignalR
{
    public class AuditHubFilter : IHubFilter
    {
        public Func<SignalrEventIncoming, bool> IncomingEventsFilter { get; set; }
        public Func<SignalrEventConnect, bool> ConnectEventsFilter { get; set; }
        public Func<SignalrEventDisconnect, bool> DisconnectEventsFilter { get; set; }

        public IAuditDataProvider? AuditDataProvider { get; set; }
        public EventCreationPolicy? CreationPolicy { get; set; }
        public string AuditEventType { get; set; }
        public bool AuditDisabled { get; set; }
        public bool IncludeHeaders { get; set; }
        public bool IncludeQueryString { get; set; }

        internal const string AuditScopeKey = "__private_SignalrAuditScope__";

        /// <summary>
        /// Creates a new AuditHubFilter
        /// </summary>
#pragma warning disable CS8618
        public AuditHubFilter() { }
#pragma warning restore CS8618

        /// <summary>
        /// Creates a new AuditHubFilter using the fluent configuration API.
        /// </summary>
        /// <param name="builder">Filter configuration as a fluent API</param>
        public AuditHubFilter(Action<IAuditHubConfigurator> builder)
        {
            var config = new AuditHubConfigurator();
            builder.Invoke(config);
            var filters = config._filters as AuditHubFilterConfigurator ?? new AuditHubFilterConfigurator();

            AuditDisabled = config._auditDisabled;
            AuditEventType = config._eventType;
            IncludeHeaders = config._includeHeaders;
            IncludeQueryString = config._includeQueryString;
            AuditDataProvider = config._dataProvider;
            CreationPolicy = config._creationPolicy;
            ConnectEventsFilter = filters._connectEventsFilter;
            DisconnectEventsFilter = filters._disconnectEventsFilter;
            IncomingEventsFilter = filters._incomingEventsFilter;
        }

        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            if (AuditDisabled)
            {
                return await next.Invoke(invocationContext);
            }
            
            var ev = new SignalrEventIncoming()
            {
                ConnectionId = invocationContext.Context.ConnectionId,
                Args = invocationContext.HubMethodArguments.ToList(),
                Headers = IncludeHeaders ? invocationContext.Context.GetHttpContext()?.Request.Headers?.ToDictionary(k => k.Key, v => v.Value.ToString()) : null,
                QueryString = IncludeQueryString ? QueryHelpers.ParseQuery(invocationContext.Context.GetHttpContext()?.Request.QueryString.ToString()).ToDictionary(k => k.Key, v => v.Value.ToString()) : null,
                IdentityName = invocationContext.Context.UserIdentifier,
                HubName = invocationContext.Hub.GetType().Name,
                HubType = invocationContext.Hub.GetType().FullName,
                MethodName = invocationContext.HubMethod.Name,
                MethodSignature = invocationContext.HubMethod.ToString(),
                Hub = invocationContext.Hub
            };

            if (AuditEventEnabled(ev))
            {
                await using (await CreateAuditScopeAsync(ev, invocationContext.Context.ConnectionAborted))
                {
                    try
                    {
                        ev.Result = await next.Invoke(invocationContext);
                        return ev.Result;
                    }
                    catch (Exception ex)
                    {
                        ev.Exception = ex.ToString();
                        throw;
                    }
                }
            }

            return await next.Invoke(invocationContext);
        }

        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            if (AuditDisabled)
            {
                return;
            }

            var ev = new SignalrEventConnect()
            {
                ConnectionId = context.Context.ConnectionId,
                Headers = IncludeHeaders ? context.Context.GetHttpContext()?.Request.Headers?.ToDictionary(k => k.Key, v => v.Value.ToString()) : null,
                QueryString = IncludeQueryString ? QueryHelpers.ParseQuery(context.Context.GetHttpContext()?.Request.QueryString.ToString()).ToDictionary(k => k.Key, v => v.Value.ToString()) : null,
                IdentityName = context.Context.UserIdentifier,
                Hub = context.Hub
            };

            if (AuditEventEnabled(ev))
            {
                var scope = await CreateAuditScopeAsync(ev, context.Context.ConnectionAborted);
                await scope.DisposeAsync();
            }

            await next.Invoke(context);
        }

        public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
        {
            if (AuditDisabled)
            {
                return;
            }

            var ev = new SignalrEventDisconnect()
            {
                ConnectionId = context.Context.ConnectionId,
                Headers = IncludeHeaders ? context.Context.GetHttpContext()?.Request.Headers?.ToDictionary(k => k.Key, v => v.Value.ToString()) : null,
                QueryString = IncludeQueryString ? QueryHelpers.ParseQuery(context.Context.GetHttpContext()?.Request.QueryString.ToString()).ToDictionary(k => k.Key, v => v.Value.ToString()) : null,
                IdentityName = context.Context.UserIdentifier,
                Exception = exception?.ToString(),
                Hub = context.Hub
            };

            if (AuditEventEnabled(ev))
            {
                await using (await CreateAuditScopeAsync(ev, context.Context.ConnectionAborted))
                {
                    await next.Invoke(context, exception);
                    return;
                }
            }

            await next.Invoke(context, exception);
        }

        private bool AuditEventEnabled(SignalrEventBase ev)
        {
            return ev switch
            {
                SignalrEventIncoming incoming => IncomingEventsFilter?.Invoke(incoming) ?? true,
                SignalrEventConnect connect => ConnectEventsFilter?.Invoke(connect) ?? true,
                SignalrEventDisconnect disconnect => DisconnectEventsFilter?.Invoke(disconnect) ?? true,
                _ => true
            };
        }

        private async Task<IAuditScope> CreateAuditScopeAsync(SignalrEventBase signalrEvent, CancellationToken cancellationToken)
        {
            var auditEvent = new AuditEventSignalr()
            {
                Event = signalrEvent
            };

            // Try to get IAuditScopeFactory / DataProvider as registered services
            var httpContext = signalrEvent.Hub.Context.GetHttpContext();
            var scopeFactory = httpContext?.RequestServices?.GetService<IAuditScopeFactory>() ?? Core.Configuration.AuditScopeFactory;
            var dataProvider = AuditDataProvider ?? httpContext?.RequestServices?.GetService<IAuditDataProvider>() ?? httpContext?.RequestServices?.GetService<AuditDataProvider>();
            
            var scope = await scopeFactory.CreateAsync(new AuditScopeOptions()
            {
                EventType = (AuditEventType ?? (signalrEvent is SignalrEventIncoming ? "{hub}.{method}" : "{event}"))
                    .Replace("{event}", signalrEvent.EventType.ToString())
                    .Replace("{hub}", (signalrEvent as SignalrEventIncoming)?.HubName)
                    .Replace("{method}", (signalrEvent as SignalrEventIncoming)?.MethodName),
                AuditEvent = auditEvent,
                DataProvider = dataProvider,
                CreationPolicy = CreationPolicy
            }, cancellationToken);

            if (httpContext != null)
            {
                httpContext.Items[AuditScopeKey] = scope;
            }

            return scope;
        }
    }
}
#endif