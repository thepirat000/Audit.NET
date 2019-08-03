using System;
using Audit.Core;
using System.Reflection;
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET461
using Microsoft.EntityFrameworkCore;
#elif NET45
using System.Data.Entity;
#endif


namespace Audit.EntityFramework.ConfigurationApi
{
    public class EntityFrameworkProviderConfigurator : IEntityFrameworkProviderConfigurator, IEntityFrameworkProviderConfiguratorAction, IEntityFrameworkProviderConfiguratorExtra
    {
        internal bool _ignoreMatchedProperties = false;
        internal Func<Type, EventEntry, Type> _auditTypeMapper;
        internal Func<AuditEvent, EventEntry, object, bool> _auditEntityAction;
        internal Func<AuditEventEntityFramework, DbContext> _dbContextBuilder;

        public IEntityFrameworkProviderConfigurator UseDbContext(Func<AuditEventEntityFramework, DbContext> dbContextBuilder)
        {
            _dbContextBuilder = dbContextBuilder;
            return this;
        }

        public IEntityFrameworkProviderConfigurator UseDbContext<T>(params object[] constructorArgs) where T : DbContext
        {
            _dbContextBuilder = ev => (T)Activator.CreateInstance(typeof(T), constructorArgs);
            return this;
        }

        public IEntityFrameworkProviderConfiguratorAction AuditTypeMapper(Func<Type, Type> mapper)
        {
            _auditTypeMapper = (t, e) => mapper.Invoke(t);
            return this;
        }

        public IEntityFrameworkProviderConfiguratorAction AuditTypeNameMapper(Func<string, string> mapper)
        {
            _auditTypeMapper = (t, e) =>
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
            _auditEntityAction = (ev, ent, obj) =>
            {
                action.Invoke(ev, ent, obj);
                return true;
            };
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, bool> function)
        {
            _auditEntityAction = function;
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> action)
        {
            _auditEntityAction = (ev, ent, obj) =>
            {
                if (obj is T)
                {
                    action.Invoke(ev, ent, (T)obj);
                }
                return true;
            };
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, bool> function)
        {
            _auditEntityAction = (ev, ent, obj) =>
            {
                if (obj is T)
                {
                    return function.Invoke(ev, ent, (T)obj);
                }
                return true;
            };
            return this;
        }

        public void IgnoreMatchedProperties(bool ignore = true)
        {
            _ignoreMatchedProperties = ignore;
        }
    }
}
