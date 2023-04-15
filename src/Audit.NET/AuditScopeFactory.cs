using Audit.Core.Providers;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    /// <summary>
    /// A factory of scopes.
    /// </summary>
    public class AuditScopeFactory : IAuditScopeFactory
    {
        #region IAuditScopeFactory implementation
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(AuditScopeOptions options)
        {
            return new AuditScope(options).Start();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IAuditScope> CreateAsync(AuditScopeOptions options, CancellationToken cancellationToken = default)
        {
            return await new AuditScope(options).StartAsync(cancellationToken);
        }
        #endregion

        /// <summary>
        /// Creates an audit scope with the given creation options as a Fluent API.
        /// </summary>
        /// <param name="config">Fluent API to configure the Audit Scope creation options</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(Action<IAuditScopeOptionsConfigurator> config)
        {
            var options = new AuditScopeOptions(config);
            return new AuditScope(options).Start();
        }

        /// <summary>
        /// Creates an audit scope with the given creation options as a Fluent API.
        /// </summary>
        /// <param name="config">Fluent API to configure the Audit Scope creation options</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IAuditScope> CreateAsync(Action<IAuditScopeOptionsConfigurator> config, CancellationToken cancellationToken = default)
        {
            var options = new AuditScopeOptions(config);
            return await new AuditScope(options).StartAsync(cancellationToken);
        }

        /// <summary>
        /// Creates an audit scope with the given extra fields, and saves it right away using the globally configured data provider. Shortcut for CreateAndSave().
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Log(string eventType, object extraFields)
        {
            using (var scope = new AuditScope(new AuditScopeOptions(eventType, null, extraFields, null, null, true)))
            {
                scope.Start();
            }
        }
        /// <summary>
        /// Creates an audit scope with the given extra fields, and saves it right away using the globally configured data provider. Shortcut for CreateAndSaveAsync().
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogAsync(string eventType, object extraFields, CancellationToken cancellationToken = default)
        {
            using (var scope = new AuditScope(new AuditScopeOptions(eventType, null, extraFields, null, null, true)))
            {
                await scope.StartAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Creates an audit scope for a target object and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(string eventType, Func<object> target)
        {
            return new AuditScope(new AuditScopeOptions(eventType, target, null, null, null)).Start();
        }
        /// <summary>
        /// Creates an audit scope for a target object and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        [MethodImpl(MethodImplOptions.NoInlining)] 
        public async Task<IAuditScope> CreateAsync(string eventType, Func<object> target, CancellationToken cancellationToken = default)
        {
            return await new AuditScope(new AuditScopeOptions(eventType, target, null, null, null)).StartAsync(cancellationToken);
        }

        /// <summary>
        /// Creates an audit scope for a target object and an event type, providing the creation policy and optionally the Data Provider.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(string eventType, Func<object> target, EventCreationPolicy creationPolicy, AuditDataProvider dataProvider)
        {
            return new AuditScope(new AuditScopeOptions(eventType, target, null, dataProvider, creationPolicy)).Start();
        }
        /// <summary>
        /// Creates an audit scope for a target object and an event type, providing the creation policy and optionally the Data Provider.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="creationPolicy">The event creation policy to use. NULL to use the configured default policy.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IAuditScope> CreateAsync(string eventType, Func<object> target, EventCreationPolicy? creationPolicy, AuditDataProvider dataProvider, CancellationToken cancellationToken = default)
        {
            return await new AuditScope(new AuditScopeOptions(eventType, target, null, dataProvider, creationPolicy)).StartAsync(cancellationToken);
        }

        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        /// <param name="creationPolicy">The event creation policy to use. NULL to use the configured default policy.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(string eventType, Func<object> target, object extraFields, EventCreationPolicy? creationPolicy, AuditDataProvider dataProvider)
        {
            return new AuditScope(new AuditScopeOptions(eventType, target, extraFields, dataProvider, creationPolicy)).Start();
        }
        /// <summary>
        /// Creates an audit scope from a reference value, and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        /// <param name="creationPolicy">The event creation policy to use. NULL to use the configured default policy.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IAuditScope> CreateAsync(string eventType, Func<object> target, object extraFields, EventCreationPolicy? creationPolicy, AuditDataProvider dataProvider, CancellationToken cancellationToken = default)
        {
            return await new AuditScope(new AuditScopeOptions(eventType, target, extraFields, dataProvider, creationPolicy)).StartAsync(cancellationToken);
        }

    }
}
