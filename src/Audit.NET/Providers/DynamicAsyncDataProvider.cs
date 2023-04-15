using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core.Providers
{
    /// <summary>
    /// A dynamic provider that lets you create a an audit data provider on the fly by specifying the actions to take as lambda expressions.
    /// </summary>
    public class DynamicAsyncDataProvider : AuditDataProvider
    {
        private readonly List<Func<AuditEvent, CancellationToken, Task<object>>> _onInsert = new List<Func<AuditEvent, CancellationToken, Task<object>>>();
        private readonly List<Func<object, AuditEvent, CancellationToken, Task>> _onReplace = new List<Func<object, AuditEvent, CancellationToken, Task>>();

        public DynamicAsyncDataProvider()
        {
        }

        public DynamicAsyncDataProvider(Action<ConfigurationApi.IDynamicAsyncDataProviderConfigurator> config)
        {
            var dataProvider = new DynamicAsyncDataProvider();
            var dynConfig = new ConfigurationApi.DynamicAsyncDataProviderConfigurator(dataProvider);
            if (config != null)
            {
                config.Invoke(dynConfig);
                _onInsert = dynConfig._dynamicAsyncDataProvider._onInsert;
                _onReplace = dynConfig._dynamicAsyncDataProvider._onReplace;
            }
        }

        /// <summary>
        /// Attaches a function to be executed by the InsertEvent method.
        /// </summary>
        /// <param name="insertFunction">The insert function.</param>
        public void AttachOnInsert(Func<AuditEvent, Task<object>> insertFunction)
        {
            _onInsert.Add(async (ev, _) => await insertFunction.Invoke(ev));
        }

        /// <summary>
        /// Attaches a function to be executed by the InsertEvent method.
        /// </summary>
        /// <param name="insertFunction">The insert function.</param>
        public void AttachOnInsert(Func<AuditEvent, CancellationToken, Task<object>> insertFunction)
        {
            _onInsert.Add(insertFunction);
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent method that will return a random Guid as the event id.
        /// </summary>
        /// <param name="insertAction">The insert action.</param>
        public void AttachOnInsert(Func<AuditEvent, Task> insertAction)
        {
            _onInsert.Add(async (ev, _) => { await insertAction.Invoke(ev); return Guid.NewGuid(); });
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent method that will return a random Guid as the event id.
        /// </summary>
        /// <param name="insertAction">The insert action.</param>
        public void AttachOnInsert(Func<AuditEvent, CancellationToken, Task> insertAction)
        {
            _onInsert.Add(async (ev, ct) => { await insertAction.Invoke(ev, ct); return Guid.NewGuid(); });
        }

        /// <summary>
        /// Attaches an action to be executed by the ReplaceEvent method.
        /// </summary>
        /// <param name="replaceAction">The replace action.</param>
        public void AttachOnReplace(Func<object, AuditEvent, Task> replaceAction)
        {
            _onReplace.Add(async (id, ev, _) => await replaceAction.Invoke(id, ev));
        }

        /// <summary>
        /// Attaches an action to be executed by the ReplaceEvent method.
        /// </summary>
        /// <param name="replaceAction">The replace action.</param>
        public void AttachOnReplace(Func<object, AuditEvent, CancellationToken, Task> replaceAction)
        {
            _onReplace.Add(replaceAction);
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="action">The action.</param>
        public void AttachOnInsertAndReplace(Func<AuditEvent, Task> action)
        {
            _onInsert.Add(async (ev, _) => { await action.Invoke(ev); return Guid.NewGuid(); });
            _onReplace.Add(async (obj, ev, _) => await action.Invoke(ev));
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="action">The action.</param>
        public void AttachOnInsertAndReplace(Func<AuditEvent, CancellationToken, Task> action)
        {
            _onInsert.Add(async (ev, ct) => { await action.Invoke(ev, ct); return Guid.NewGuid(); });
            _onReplace.Add(async (obj, ev, ct) => await action.Invoke(ev, ct));
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods, the first parameter (event id) will be NULL in case of insert.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="action">The action.</param>
        public void AttachOnInsertAndReplace(Func<object, AuditEvent, Task> action)
        {
            _onInsert.Add(async (ev, ct) => { await action.Invoke(null, ev); return Guid.NewGuid(); });
            _onReplace.Add(async (obj, ev, ct) => await action.Invoke(obj, ev));
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods, the first parameter (event id) will be NULL in case of insert.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="action">The action.</param>
        public void AttachOnInsertAndReplace(Func<object, AuditEvent, CancellationToken, Task> action)
        {
            _onInsert.Add(async (ev, ct) => { await action.Invoke(null, ev, ct); return Guid.NewGuid(); });
            _onReplace.Add(async (obj, ev, ct) => await action.Invoke(obj, ev, ct));
        }

        /// <summary>
        /// Inserts the event by invoking the attached actions.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            object result = null;
            foreach (var action in _onInsert)
            {
                result = await action.Invoke(auditEvent, cancellationToken);
            }
            return result;
        }

        /// <summary>
        /// Replaces the event by invocating the attached actions.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            foreach (var action in _onReplace)
            {
                await action.Invoke(eventId, auditEvent, cancellationToken);
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            return InsertEventAsync(auditEvent).GetAwaiter().GetResult();
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            ReplaceEventAsync(eventId, auditEvent).GetAwaiter().GetResult();
        }
    }
}
