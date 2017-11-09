using System;
using System.Collections.Generic;
using Audit.Core;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class AuditEntityMapping : IAuditEntityMapping
    {
        private Dictionary<Type, Type> _mapping = new Dictionary<Type, Type>();
        private Dictionary<Type, Action<AuditEvent, EventEntry, object>> _actions = new Dictionary<Type, Action<AuditEvent, EventEntry, object>>();
        private Action<AuditEvent, EventEntry, object> _commonAction = null;

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>()
        {
            _mapping.Add(typeof(TSourceEntity), typeof(TAuditEntity));
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<AuditEvent, EventEntry, TAuditEntity> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = typeof(TAuditEntity);
            _actions[typeof(TAuditEntity)] = (ev, ent, auditEntity) => entityAction.Invoke(ev, ent, (TAuditEntity)auditEntity);
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<TSourceEntity, TAuditEntity> entityAction)
        {
            _mapping.Add(typeof(TSourceEntity), typeof(TAuditEntity));
            _actions[typeof(TAuditEntity)] = (ev, ent, auditEntity) => entityAction.Invoke((TSourceEntity)ent.Entry.Entity, (TAuditEntity)auditEntity);
            return this;
        }

        public void AuditEntityAction(Action<AuditEvent, EventEntry, object> entityAction)
        {
            _commonAction = entityAction;
        }

        public void AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> entityAction)
        {
            _commonAction = (ev, ent, auditEntity) => entityAction.Invoke(ev, ent, (T)auditEntity);
        }

        internal Func<Type, Type> GetMapper()
        {
            return t =>
            {
                Type mappedType = null;
                _mapping.TryGetValue(t, out mappedType);
                return mappedType;
            };
        }

        internal Action<AuditEvent, EventEntry, object> GetAction()
        {
            return (ev, ent, auditEntity) =>
            {
                Action<AuditEvent, EventEntry, object> action = null;
                if (_actions.TryGetValue(auditEntity.GetType(), out action))
                {
                    action.Invoke(ev, ent, auditEntity);
                }
                if (_commonAction != null)
                {
                    _commonAction.Invoke(ev, ent, auditEntity);
                }
            };
        }
    }
}
