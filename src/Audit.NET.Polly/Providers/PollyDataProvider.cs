using System;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Polly.Configuration;
using Polly;

namespace Audit.Polly.Providers
{
    /// <summary>
    /// A data provider that uses Polly for resilience
    /// </summary>
    public class PollyDataProvider : AuditDataProvider
    {
        /// <summary>
        /// The primary data provider
        /// </summary>
        public AuditDataProvider? PrimaryDataProvider { get; set; }

        /// <summary>
        /// The resilience pipeline
        /// </summary>
        public ResiliencePipeline<object>? ResiliencePipeline { get; set; }

        public PollyDataProvider()
        {
        }
        
        public PollyDataProvider(Action<IPollyProviderConfigurator> config)
        {
            var cfg = new PollyProviderConfigurator();
            config?.Invoke(cfg);
            ResiliencePipeline = cfg._resilienceConfigurator!._pipeline;
            PrimaryDataProvider = cfg._resilienceConfigurator._innerDataProvider;
        }

        public PollyDataProvider(ResiliencePipeline<object> resiliencePipeline, AuditDataProvider primaryDataProvider)
        {
            ResiliencePipeline = resiliencePipeline;
            PrimaryDataProvider = primaryDataProvider;
        }

        /// <summary>
        /// Creates a new resilience context for Insert or Replace operations
        /// </summary>
        /// <param name="operationKey">The operation key</param>
        /// <param name="auditEvent">The Audit Event</param>
        /// <param name="eventId">The Event ID in case of Replace</param>
        /// <param name="cancellationToken">The cancellation token</param>
        protected virtual ResilienceContext CreateResilienceContext(string operationKey, AuditEvent auditEvent, object? eventId, CancellationToken cancellationToken)
        {
            var context = ResilienceContextPool.Shared.Get(operationKey, cancellationToken);

            context.Properties.Set(new ResiliencePropertyKey<object?>("EventId"), eventId);
            context.Properties.Set(new ResiliencePropertyKey<AuditEvent>("AuditEvent"), auditEvent);

            return context;
        }

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var context = CreateResilienceContext(nameof(InsertEvent), auditEvent, null, new CancellationToken());

            return ResiliencePipeline!.Execute<object>(ctx => PrimaryDataProvider!.InsertEvent(auditEvent), context);
        }

        /// <inheritdoc />
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var context = CreateResilienceContext(nameof(InsertEventAsync), auditEvent, null, cancellationToken);

            return await ResiliencePipeline!.ExecuteAsync<object>(async ctx => await PrimaryDataProvider!.InsertEventAsync(auditEvent, ctx.CancellationToken), context);
        }

        /// <inheritdoc />
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var context = CreateResilienceContext(nameof(ReplaceEvent), auditEvent, eventId, new CancellationToken());

            ResiliencePipeline!.Execute<object>(ctx =>
            {
                PrimaryDataProvider!.ReplaceEvent(eventId, auditEvent);

                return new object();
            }, context);
        }

        /// <inheritdoc />
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var context = CreateResilienceContext(nameof(ReplaceEventAsync), auditEvent, eventId, cancellationToken);

            await ResiliencePipeline!.ExecuteAsync<object>(async ctx =>
            {
                await PrimaryDataProvider!.ReplaceEventAsync(eventId, auditEvent, ctx.CancellationToken);

                return new object();
            }, context);
        }

        /// <inheritdoc />
        public override object CloneValue<T>(T value, AuditEvent auditEvent)
        {
            return PrimaryDataProvider!.CloneValue(value, auditEvent);
        }

        /// <inheritdoc />
        public override T GetEvent<T>(object eventId)
        {
            return PrimaryDataProvider!.GetEvent<T>(eventId);
        }

        /// <inheritdoc />
        public override Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            return PrimaryDataProvider!.GetEventAsync<T>(eventId, cancellationToken);
        }
    }
}