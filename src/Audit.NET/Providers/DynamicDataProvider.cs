using System;
using System.Collections.Generic;

namespace Audit.Core.Providers
{
    /// <summary>
    /// A dynamic provider that lets you create a an audit data provider on the fly by specifying the actions to take as lambda expressions.
    /// </summary>
    public class DynamicDataProvider : AuditDataProvider
    {
        private List<Func<AuditEvent, object>> _onInsert = new List<Func<AuditEvent, object>>();
        private List<Action<object, AuditEvent>> _onReplace = new List<Action<object, AuditEvent>>();

        public DynamicDataProvider()
        {
        }

        public DynamicDataProvider(Action<ConfigurationApi.IDynamicDataProviderConfigurator> config)
        {
            var dataProvider = new DynamicDataProvider();
            var dynConfig = new ConfigurationApi.DynamicDataProviderConfigurator(dataProvider);
            if (config != null)
            {
                config.Invoke(dynConfig);
                _onInsert = dynConfig._dynamicDataProvider._onInsert;
                _onReplace = dynConfig._dynamicDataProvider._onReplace;
            }
        }

        /// <summary>
        /// Attaches a function to be executed by the InsertEvent method.
        /// </summary>
        /// <param name="insertFunction">The insert function.</param>
        public void AttachOnInsert(Func<AuditEvent, object> insertFunction)
        {
            _onInsert.Add(insertFunction);
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent method that will return a random Guid as the event id.
        /// </summary>
        /// <param name="insertAction">The insert action.</param>
        public void AttachOnInsert(Action<AuditEvent> insertAction)
        {
            _onInsert.Add(new Func<AuditEvent, object>(ev => { insertAction.Invoke(ev); return Guid.NewGuid(); }));
        }

        /// <summary>
        /// Attaches an action to be executed by the ReplaceEvent method.
        /// </summary>
        /// <param name="replaceAction">The replace action.</param>
        public void AttachOnReplace(Action<object, AuditEvent> replaceAction)
        {
            _onReplace.Add(replaceAction);
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="action">The action.</param>
        public void AttachOnInsertAndReplace(Action<AuditEvent> action)
        {
            _onInsert.Add(new Func<AuditEvent, object>(ev => { action.Invoke(ev); return Guid.NewGuid(); }));
            _onReplace.Add((obj, ev) => action.Invoke(ev));
        }

        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods, the first parameter (event id) will be NULL in case of insert.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="action">The action.</param>
        public void AttachOnInsertAndReplace(Action<object, AuditEvent> action)
        {
            _onInsert.Add(new Func<AuditEvent, object>(ev => { action.Invoke(null, ev); return Guid.NewGuid(); }));
            _onReplace.Add(action);
        }

        /// <summary>
        /// Inserts the event by invocating the attached actions.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            object result = null;
            foreach (var action in _onInsert)
            {
                result = action.Invoke(auditEvent);
            }
            return result;
        }

        /// <summary>
        /// Replaces the event by invocating the attached actions.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="auditEvent">The audit event.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            foreach (var action in _onReplace)
            {
                action.Invoke(eventId, auditEvent);
            }
        }
    }
}
