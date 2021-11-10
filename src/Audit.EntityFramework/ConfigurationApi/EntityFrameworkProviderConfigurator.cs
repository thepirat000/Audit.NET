﻿using System;
using Audit.Core;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
#if EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif


namespace Audit.EntityFramework.ConfigurationApi
{
    public class EntityFrameworkProviderConfigurator : IEntityFrameworkProviderConfigurator, IEntityFrameworkProviderConfiguratorAction, IEntityFrameworkProviderConfiguratorExtra
    {
        internal Func<Type, bool> _ignoreMatchedPropertiesFunc = t => false;
        internal Func<Type, EventEntry, Type> _auditTypeMapper;
        internal Func<AuditEvent, EventEntry, object, Task<bool>> _auditEntityAction;
        internal Func<AuditEventEntityFramework, DbContext> _dbContextBuilder;
        internal Func<EventEntry, Type> _explicitMapper;

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
            _explicitMapper = mapping.GetExplicitMapper();
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Action<AuditEvent, EventEntry, object> action)
        {
            _auditEntityAction = (ev, ent, obj) =>
            {
                action.Invoke(ev, ent, obj);
                return Task.FromResult(true);
            };
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, Task> asyncAction)
        {
            _auditEntityAction = async (ev, ent, obj) =>
            {
                await asyncAction.Invoke(ev, ent, obj);
                return true;
            };
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, bool> function)
        {
            _auditEntityAction = (ev, ent, obj) => Task.FromResult(function.Invoke(ev, ent, obj));
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, Task<bool>> asyncFunction)
        {
            _auditEntityAction = async (ev, ent, obj) => await asyncFunction.Invoke(ev, ent, obj);
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
                return Task.FromResult(true);
            };
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task> asyncAction)
        {
            _auditEntityAction = async (ev, ent, obj) =>
            {
                if (obj is T)
                {
                    await asyncAction.Invoke(ev, ent, (T)obj);
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
                    return Task.FromResult(function.Invoke(ev, ent, (T)obj));
                }
                return Task.FromResult(true);
            };
            return this;
        }

        public IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task<bool>> asyncFunction)
        {
            _auditEntityAction = async (ev, ent, obj) =>
            {
                if (obj is T)
                {
                    return await asyncFunction.Invoke(ev, ent, (T)obj);
                }
                return true;
            };
            return this;
        }

        public void IgnoreMatchedProperties(bool ignore = true)
        {
            _ignoreMatchedPropertiesFunc = _ => ignore;
        }

        public void IgnoreMatchedProperties(Func<Type, bool> ignoreFunc)
        {
            _ignoreMatchedPropertiesFunc = ignoreFunc;
        }
    }
}
