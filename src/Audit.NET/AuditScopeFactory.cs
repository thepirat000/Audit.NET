using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    /// <summary>
    /// The default factory of audit scopes. 
    /// </summary>
    public class AuditScopeFactory : IAuditScopeFactory
    {
        /// <summary>
        /// Override this method to customize the configuration of audit scopes prior to their creation.
        /// </summary>
        /// <param name="options">The audit scope options.</param>
        public virtual void OnConfiguring(AuditScopeOptions options)
        {
        }

        /// <summary>
        /// Override this method to implement custom logic for the audit scope after its creation.
        /// This is executed before the global OnScopeCreated custom actions
        /// </summary>
        /// <param name="auditScope">The audit scope created.</param>
        public virtual void OnScopeCreated(AuditScope auditScope)
        {
        }

        #region IAuditScopeFactory

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(AuditScopeOptions options)
        {
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return auditScope.Start();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IAuditScope> CreateAsync(AuditScopeOptions options, CancellationToken cancellationToken = default)
        {
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return await auditScope.StartAsync(cancellationToken);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(Action<IAuditScopeOptionsConfigurator> config)
        {
            var options = new AuditScopeOptions(config);
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return auditScope.Start();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IAuditScope> CreateAsync(Action<IAuditScopeOptionsConfigurator> config, CancellationToken cancellationToken = default)
        {
            var options = new AuditScopeOptions(config);
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return await auditScope.StartAsync(cancellationToken);
        }

        #endregion

        /// <summary>
        /// Creates an audit scope for a target object and an event type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IAuditScope Create(string eventType, Func<object> target)
        {
            var options = new AuditScopeOptions() { EventType = eventType, TargetGetter = target };
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return auditScope.Start();
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
            var options = new AuditScopeOptions() { EventType = eventType, TargetGetter = target };
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return await auditScope.StartAsync(cancellationToken);
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
            var options = new AuditScopeOptions() { EventType = eventType, TargetGetter = target, DataProvider = dataProvider, CreationPolicy = creationPolicy };
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return auditScope.Start();
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
            var options = new AuditScopeOptions() { EventType = eventType, TargetGetter = target, DataProvider = dataProvider, CreationPolicy = creationPolicy };
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return await auditScope.StartAsync(cancellationToken);
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
            var options = new AuditScopeOptions() { EventType = eventType, TargetGetter = target, ExtraFields = extraFields, DataProvider = dataProvider, CreationPolicy = creationPolicy };
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return auditScope.Start();
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
            var options = new AuditScopeOptions() { EventType = eventType, TargetGetter = target, ExtraFields = extraFields, DataProvider = dataProvider, CreationPolicy = creationPolicy };
            OnConfiguring(options);
            var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            return await auditScope.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Creates an audit scope with the given extra fields, and saves it right away using the globally configured data provider. Shortcut for CreateAndSave().
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Log(string eventType, object extraFields)
        {
            var options = new AuditScopeOptions() { EventType = eventType, ExtraFields = extraFields, IsCreateAndSave = true };
            OnConfiguring(options);
            using var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            auditScope.Start();
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
            var options = new AuditScopeOptions() { EventType = eventType, ExtraFields = extraFields, IsCreateAndSave = true };
            OnConfiguring(options);
            await using var auditScope = new AuditScope(options);
            OnScopeCreated(auditScope);
            await auditScope.StartAsync(cancellationToken);
        }
    }
}
