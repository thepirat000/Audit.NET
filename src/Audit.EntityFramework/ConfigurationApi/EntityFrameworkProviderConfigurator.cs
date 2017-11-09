using System;
using Audit.Core;
using System.Reflection;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class EntityFrameworkProviderConfigurator : IEntityFrameworkProviderConfigurator, IEntityFrameworkProviderConfiguratorAction, IEntityFrameworkProviderConfiguratorExtra
    {
        internal bool _ignoreMatchedProperties = false;
        internal Func<Type, Type> _auditTypeMapper;
        internal Action<AuditEvent, EventEntry, object> _auditEntityAction;

        public IEntityFrameworkProviderConfiguratorAction AuditTypeMapper(Func<Type, Type> mapper)
        {
            _auditTypeMapper = mapper;
            return this;
        }

        public IEntityFrameworkProviderConfiguratorAction AuditTypeNameMapper(Func<string, string> mapper)
        {
            _auditTypeMapper = t =>
            {
                var mappedTypeName = mapper.Invoke(t.Name);
                Type mappedType = null;
                if (!string.IsNullOrWhiteSpace(mappedTypeName))
                {
                    var aqTypeName = $"{t.Namespace}.{mappedTypeName}, {t.GetTypeInfo().Assembly.FullName}";
                    mappedType = Type.GetType(aqTypeName, false);
                }
                return mappedType;
            };

            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditTypeExplicitMapper(Action<IAuditEntityMapping> config)
        {
            var mapping = new AuditEntityMapping();
            config.Invoke(mapping);
            _auditTypeMapper = mapping.GetMapper();
            _auditEntityAction = mapping.GetAction();
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Action<AuditEvent, EventEntry, object> action)
        {
            _auditEntityAction = action;
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> action)
        {
            _auditEntityAction = new Action<AuditEvent, EventEntry, object>((ev, ent, obj) =>
            {
                if (obj is T)
                {
                    action.Invoke(ev, ent, (T)obj);
                }
            });
            return this;
        }

        public void IgnoreMatchedProperties(bool ignore = false)
        {
            _ignoreMatchedProperties = ignore;
        }
    }
}
