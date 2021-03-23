using System;
using System.Collections.Generic;
using Audit.Core;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class AuditEntityMapping : IAuditEntityMapping
    {
        private Dictionary<Type, MappingInfo> _mapping = new Dictionary<Type, MappingInfo>();
        private List<KeyValuePair<Func<EventEntry, bool>, MappingInfo>> _explicitMapping = new List<KeyValuePair<Func<EventEntry, bool>, MappingInfo>>();
        private Func<AuditEvent, EventEntry, object, bool> _commonAction = null;
        

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>()
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo() { TargetType = typeof(TAuditEntity) };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<AuditEvent, EventEntry, TAuditEntity> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetType = typeof(TAuditEntity),
                Action = (ev, ent, auditEntity) =>
                {
                    entityAction.Invoke(ev, ent, (TAuditEntity)auditEntity);
                    return true;
                }
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Func<AuditEvent, EventEntry, TAuditEntity, bool> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetType = typeof(TAuditEntity),
                Action = (ev, ent, auditEntity) => entityAction.Invoke(ev, ent, (TAuditEntity)auditEntity)
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<TSourceEntity, TAuditEntity> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetType = typeof(TAuditEntity),
                Action = (ev, ent, auditEntity) =>
                {
                    entityAction.Invoke((TSourceEntity)ent.Entry.Entity, (TAuditEntity)auditEntity);
                    return true;
                }
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Func<TSourceEntity, TAuditEntity, bool> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetType = typeof(TAuditEntity),
                Action = (ev, ent, auditEntity) => entityAction.Invoke((TSourceEntity)ent.Entry.Entity, (TAuditEntity)auditEntity)
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity>(Func<EventEntry, Type> mapper, Func<AuditEvent, EventEntry, object, bool> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetTypeMapper = mapper,
                Action = entityAction
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity>(Func<EventEntry, Type> mapper, Action<AuditEvent, EventEntry, object> entityAction)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetTypeMapper = mapper,
                Action = (ev, entry, entity) =>
                {
                    entityAction.Invoke(ev, entry, entity);
                    return true;
                }
            };
            return this;
        }

        public IAuditEntityMapping Map<TSourceEntity>(Func<EventEntry, Type> mapper)
        {
            _mapping[typeof(TSourceEntity)] = new MappingInfo()
            {
                TargetTypeMapper = mapper
            };
            return this;
        }

        public IAuditEntityMapping MapExplicit<TAuditEntity>(Func<EventEntry, bool> predicate, Action<EventEntry, TAuditEntity> entityAction = null)
        {
            _explicitMapping.Add(new KeyValuePair<Func<EventEntry, bool>, MappingInfo>(predicate, new MappingInfo()
            {
                TargetType = typeof(TAuditEntity),
                Action = (ev, entry, entity) =>
                {
                    entityAction?.Invoke(entry, (TAuditEntity)entity);
                    return true;
                }
            }));
            return this;
        }

        public IAuditEntityMapping MapTable<TAuditEntity>(string tableName, Action<EventEntry, TAuditEntity> entityAction = null)
        {
            _explicitMapping.Add(new KeyValuePair<Func<EventEntry, bool>, MappingInfo>(ent => ent.Table?.Equals(tableName) == true, new MappingInfo()
            {
                TargetType = typeof(TAuditEntity),
                Action = (ev, entry, entity) =>
                {
                    entityAction?.Invoke(entry, (TAuditEntity)entity);
                    return true;
                }
            }));
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
                entityAction.Invoke(ev, ent, (T)auditEntity);
                return true;
            };
        }

        public void AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, bool> entityAction)
        {
            _commonAction = (ev, ent, auditEntity) => entityAction.Invoke(ev, ent, (T)auditEntity);
        }

        internal Func<Type, EventEntry, Type> GetMapper()
        {
            return (t, e) =>
            {
                _mapping.TryGetValue(t, out MappingInfo map);
                return map?.TargetTypeMapper?.Invoke(e);
            };
        }

        internal Func<EventEntry, Type> GetExplicitMapper()
        {
            return (e) =>
            {
                if (_explicitMapping == null)
                {
                    return null;
                }
                foreach(var em in _explicitMapping)
                {
                    if (em.Key.Invoke(e))
                    {
                        return em.Value.TargetTypeMapper?.Invoke(e);
                    }
                }
                return null;
            };
        }

        // Returns a generic action that executes the specific action for the mapping and the common action
        internal Func<AuditEvent, EventEntry, object, bool> GetAction()
        {
            return (ev, ent, auditEntity) =>
            {
                MappingInfo map = null;
                bool include = true;
                bool mappedExplicitly = false;
                if (_explicitMapping != null)
                {
                    foreach (var em in _explicitMapping)
                    {
                        if (em.Key.Invoke(ent))
                        {
                            map = em.Value;
                            include = map.Action.Invoke(ev, ent, auditEntity);
                            mappedExplicitly = true;
                            break;
                        }
                    }
                }
                if (!mappedExplicitly)
                {
                    var entityType = ent.EntityType;
                    if (entityType != null && _mapping.TryGetValue(entityType, out map))
                    {
                        if (map.Action != null)
                        {
                            include = map.Action.Invoke(ev, ent, auditEntity);
                        }
                    }
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
