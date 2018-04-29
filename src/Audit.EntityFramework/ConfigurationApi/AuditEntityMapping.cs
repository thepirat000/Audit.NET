using System;
using System.Collections.Generic;
using Audit.Core;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class AuditEntityMapping : IAuditEntityMapping
    {
        private Dictionary<Type, Type> _mapping = new Dictionary<Type, Type>();
        private Dictionary<Type, Func<AuditEvent, EventEntry, object, bool>> _actions = new Dictionary<Type, Func<AuditEvent, EventEntry, object, bool>>();
        private Func<AuditEvent, EventEntry, object, bool> _commonAction = null;

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>()
        {
            _mapping.Add(typeof(TSourceEntity), typeof(TAuditEntity));
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<AuditEvent, EventEntry, TAuditEntity> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = typeof(TAuditEntity);
            _actions[typeof(TAuditEntity)] = (ev, ent, auditEntity) =>
            {
                entityAction.Invoke(ev, ent, (TAuditEntity) auditEntity);
                return true;
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Func<AuditEvent, EventEntry, TAuditEntity, bool> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = typeof(TAuditEntity);
            _actions[typeof(TAuditEntity)] = (ev, ent, auditEntity) => entityAction.Invoke(ev, ent, (TAuditEntity)auditEntity);
            return this;
        }


        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<TSourceEntity, TAuditEntity> entityAction)
        {
            _mapping.Add(typeof(TSourceEntity), typeof(TAuditEntity));
            _actions[typeof(TAuditEntity)] = (ev, ent, auditEntity) =>
            {
                entityAction.Invoke((TSourceEntity) ent.Entry.Entity, (TAuditEntity) auditEntity);
                return true;
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Func<TSourceEntity, TAuditEntity, bool> entityAction)
        {
            _mapping.Add(typeof(TSourceEntity), typeof(TAuditEntity));
            _actions[typeof(TAuditEntity)] = (ev, ent, auditEntity) => entityAction.Invoke((TSourceEntity)ent.Entry.Entity, (TAuditEntity)auditEntity);
            return this;
        }

        public void AuditEntityAction(Action<AuditEvent, EventEntry, object> entityAction)
        {
            _commonAction = (ev, ent, auditEntity) =>
            {
                entityAction.Invoke(ev, ent, auditEntity);
                return true;
            };
        }

        public void AuditEntityAction(Func<AuditEvent, EventEntry, object, bool> entityAction)
        {
            _commonAction = (ev, ent, auditEntity) => entityAction.Invoke(ev, ent, auditEntity);
        }

        public void AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> entityAction)
        {
            _commonAction = (ev, ent, auditEntity) =>
            {
                entityAction.Invoke(ev, ent, (T) auditEntity);
                return true;
            };
        }

        public void AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, bool> entityAction)
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

        internal Func<AuditEvent, EventEntry, object, bool> GetAction()
        {
            return (ev, ent, auditEntity) =>
            {
                Func<AuditEvent, EventEntry, object, bool> action = null;
                bool include = true;
                if (_actions.TryGetValue(auditEntity.GetType(), out action))
                {
                    include = action.Invoke(ev, ent, auditEntity);
                }
                if (include && _commonAction != null)
                {
                    include = _commonAction.Invoke(ev, ent, auditEntity);
                }
                return include;
            };
        }


    }
}
