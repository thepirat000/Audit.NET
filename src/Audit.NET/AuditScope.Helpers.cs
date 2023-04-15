using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    public partial class AuditScope
    {
        /// <summary>
        /// Shortcut to create an audit scope
        /// </summary>
        public static AuditScope Create(AuditScopeOptions options)
        {
            return new AuditScope(options).Start();
        }
        /// <summary>
        /// Shortcut to create an audit scope
        /// </summary>
        /// <param name="options">The Audit Scope creation options.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public static async Task<AuditScope> CreateAsync(AuditScopeOptions options, CancellationToken cancellationToken = default)
        {
            return await new AuditScope(options).StartAsync(cancellationToken);
        }
        /// <summary>
        /// Creates an audit scope with the given creation options as a Fluent API.
        /// </summary>
        public static IAuditScope Create(Action<IAuditScopeOptionsConfigurator> config)
        {
            var options = new AuditScopeOptions(config);
            return new AuditScope(options).Start();
        }
        /// <summary>
        /// Creates an audit scope with the given creation options as a Fluent API.
        /// </summary>
        /// <param name="config">Fluent API to set the Audit Scope options.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public static async Task<IAuditScope> CreateAsync(Action<IAuditScopeOptionsConfigurator> config, CancellationToken cancellationToken = default)
        {
            var options = new AuditScopeOptions(config);
            return await new AuditScope(options).StartAsync(cancellationToken);
        }
        /// <summary>
        /// Shortcut to create an audit scope with the given Event type and Target.
        /// </summary>
        /// <param name="eventType">A string representing the type of the event.</param>
        /// <param name="target">The target object getter.</param>
        /// <param name="extraFields">An anonymous object that contains additional fields to be merged into the audit event.</param>
        public static AuditScope Create(string eventType, Func<object> target, object extraFields = null)
        {
            var options = new AuditScopeOptions(eventType: eventType, targetGetter: target, extraFields: extraFields);
            return new AuditScope(options).Start();
        }
        /// <summary>
        /// Shortcut to create an audit scope with the given Event type and Target.
        /// </summary>
        /// <param name="eventType">A string representing the type of the event.</param>
        /// <param name="target">The target object getter.</param>
        /// <param name="extraFields">An anonymous object that contains additional fields to be merged into the audit event.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public static async Task<AuditScope> CreateAsync(string eventType, Func<object> target, object extraFields = null, CancellationToken cancellationToken = default)
        {
            var options = new AuditScopeOptions(eventType: eventType, targetGetter: target, extraFields: extraFields);
            return await new AuditScope(options).StartAsync(cancellationToken);
        }

        /// <summary>
        /// Creates an audit scope with the given extra fields, and saves it right away
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        public static void Log(string eventType, object extraFields)
        {
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                ExtraFields = extraFields,
                IsCreateAndSave = true
            };
            new AuditScope(options).Start();
        }
        /// <summary>
        /// Creates an audit scope with the given extra fields, and saves it right away
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields to be merged into the audit event.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public static async Task LogAsync(string eventType, object extraFields, CancellationToken cancellationToken = default)
        {
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                ExtraFields = extraFields,
                IsCreateAndSave = true
            };
            await new AuditScope(options).StartAsync(cancellationToken);
        }
    }
}
