using System.Linq;
using Audit.Core;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Audit.EntityFramework.ConfigurationApi;
#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
#else
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
#endif

namespace Audit.EntityFramework.Providers
{
    /// <summary>
    /// Store the audits logs in the same EntityFramework model as the audited entities.
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - AuditTypeMapper: A function that maps an entity type to its audited type (i.e. Order -> OrderAudit, etc)
    /// - AuditEntityAction: An action to perform on the audit entity before saving it
    /// - IgnoreMatchedProperties: Set to true to avoid the property values copy from the entity to the audited entity (default is false)
    /// </remarks>
    public class EntityFrameworkDataProvider : AuditDataProvider
    {
        private Func<Type, bool> _ignoreMatchedPropertiesFunc;
        private Func<Type, EventEntry, Type> _auditTypeMapper;
        private Func<AuditEvent, EventEntry, object, Task<bool>> _auditEntityAction;
        private Func<AuditEventEntityFramework, DbContext> _dbContextBuilder;
        private Func<EventEntry, Type> _explicitMapper;

        public EntityFrameworkDataProvider()
        {
        }

        public EntityFrameworkDataProvider(Action<ConfigurationApi.IEntityFrameworkProviderConfigurator> config)
        {
            var efConfig = new ConfigurationApi.EntityFrameworkProviderConfigurator();
            if (config != null)
            {
                config.Invoke(efConfig);
                _auditEntityAction = efConfig._auditEntityAction;
                _auditTypeMapper = efConfig._auditTypeMapper;
                _dbContextBuilder = efConfig._dbContextBuilder;
                _ignoreMatchedPropertiesFunc = efConfig._ignoreMatchedPropertiesFunc;
                _explicitMapper = efConfig._explicitMapper;
            }
        }

        public Func<Type, EventEntry, Type> AuditTypeMapper
        {
            get { return _auditTypeMapper; }
            set { _auditTypeMapper = value; }
        }

        /// <summary>
        /// Function to execute on the audited entities before saving them. 
        /// Returns a boolean value indicating whther to include the entity on the logs or not.
        /// </summary>
        public Func<AuditEvent, EventEntry, object, Task<bool>> AuditEntityAction
        {
            get { return _auditEntityAction; }
            set { _auditEntityAction = value; }
        }

        public Func<EventEntry, Type> ExplicitMapper
        {
            get { return _explicitMapper; }
            set { _explicitMapper = value; }
        }

        public Func<Type, bool> IgnoreMatchedPropertiesFunc
        {
            get { return _ignoreMatchedPropertiesFunc; }
            set { _ignoreMatchedPropertiesFunc = value; }
        }

        /// <summary>
        /// Function that returns the DbContext to use for Audit Tables (default is the same context as the one audited)
        /// </summary>
        public Func<AuditEventEntityFramework, DbContext> DbContextBuilder
        {
            get { return _dbContextBuilder; }
            set { _dbContextBuilder = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            bool save = false;
            if (!(auditEvent is AuditEventEntityFramework efEvent))
            {
                return null;
            }
            var localDbContext = efEvent.EntityFrameworkEvent.DbContext;
            var auditDbContext = DbContextBuilder?.Invoke(efEvent) ?? localDbContext;
            foreach(var entry in efEvent.EntityFrameworkEvent.Entries)
            {
                Type mappedType = _explicitMapper?.Invoke(entry);
                object auditEntity = null;
                if (mappedType != null)
                {
                    // Explicit mapping (Table -> Type)
                    auditEntity = CreateAuditEntityExplicit(mappedType, entry);
                }
                else
                {
                    // Implicit mapping (Type -> Type)
                    Type type = GetEntityType(entry, localDbContext);
                    if (type != null)
                    {
                        entry.EntityType = type;
                        mappedType = _auditTypeMapper?.Invoke(type, entry);
                        if (mappedType != null)
                        {
                            auditEntity = CreateAuditEntity(type, mappedType, entry);
                        }
                    }
                }
                if (auditEntity != null)
                {
                    if (_auditEntityAction == null || _auditEntityAction.Invoke(efEvent, entry, auditEntity).Result)
                    {
#if EF_FULL
                        auditDbContext.Set(mappedType).Add(auditEntity);
#else
                        auditDbContext.Add(auditEntity);
#endif
                        save = true;
                    }
                }
            }
            if (save)
            {
                if (auditDbContext is IAuditBypass)
                {
                    (auditDbContext as IAuditBypass).SaveChangesBypassAudit();
                }
                else
                {
                    auditDbContext.SaveChanges();
                }
            }
            return null;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            bool save = false;
            if (!(auditEvent is AuditEventEntityFramework efEvent))
            {
                return null;
            }
            var localDbContext = efEvent.EntityFrameworkEvent.DbContext;
            var auditDbContext = DbContextBuilder?.Invoke(efEvent) ?? localDbContext;
            foreach (var entry in efEvent.EntityFrameworkEvent.Entries)
            {
                Type entityType = GetEntityType(entry, localDbContext);
                entry.EntityType = entityType;
                Type mappedType = _explicitMapper?.Invoke(entry);
                object auditEntity = null;
                if (mappedType != null)
                {
                    // Explicit mapping (Table -> Type)
                    auditEntity = CreateAuditEntityExplicit(mappedType, entry);
                }
                else
                {
                    // Implicit mapping (Type -> Type)
                    if (entityType != null)
                    {
                        entry.EntityType = entityType;
                        mappedType = _auditTypeMapper?.Invoke(entityType, entry);
                        if (mappedType != null)
                        {
                            auditEntity = CreateAuditEntity(entityType, mappedType, entry);
                        }
                    }
                }
                if (auditEntity != null)
                {
                    if (_auditEntityAction == null || await _auditEntityAction.Invoke(efEvent, entry, auditEntity))
                    {
#if EF_FULL
                        auditDbContext.Set(mappedType).Add(auditEntity);
#else
                        await auditDbContext.AddAsync(auditEntity);
#endif
                        save = true;
                    }
                }
            }
            if (save)
            {
                if (auditDbContext is IAuditBypass)
                {
                    await (auditDbContext as IAuditBypass).SaveChangesBypassAuditAsync();
                }
                else
                {
                    await auditDbContext.SaveChangesAsync();
                }
            }
            return auditEvent;
        }

        private Type GetEntityType(EventEntry entry, DbContext localDbContext)
        {
            var entryType = entry.Entry.Entity.GetType();
            if (entryType.FullName.StartsWith("Castle.Proxies."))
            {
                entryType = entryType.GetTypeInfo().BaseType;
            }
            Type type;
#if (EF_CORE_6)
            IReadOnlyEntityType definingType = entry.Entry.Metadata.FindOwnership()?.DeclaringEntityType ?? localDbContext.Model.FindEntityType(entry.Entry.Metadata.Name);
            type = definingType?.ClrType;
#elif (EF_CORE_3 || EF_CORE_5)
            IEntityType definingType = entry.Entry.Metadata.FindOwnership()?.DeclaringEntityType ?? entry.Entry.Metadata.DefiningEntityType ?? localDbContext.Model.FindEntityType(entry.Entry.Metadata.Name);
            type = definingType?.ClrType;
#elif EF_CORE_2
            IEntityType definingType = entry.Entry.Metadata.DefiningEntityType ?? localDbContext.Model.FindEntityType(entry.Entry.Metadata.ClrType);
            type = definingType?.ClrType;
#elif EF_CORE
            IEntityType definingType = localDbContext.Model.FindEntityType(entryType);
            type = definingType?.ClrType;
#else
            type = ObjectContext.GetObjectType(entryType);
#endif
            return type;
        }

        private object CreateAuditEntity(Type definingType, Type auditType, EventEntry entry)
        {
            var entity = entry.Entry.Entity;
            var auditEntity = Activator.CreateInstance(auditType);
            if (_ignoreMatchedPropertiesFunc == null || !_ignoreMatchedPropertiesFunc(auditType))
            {
                var auditFields = GetPropertiesToSet(auditType);
                var entityFields = GetPropertiesToGet(definingType);
                foreach (var field in entityFields.Where(af => auditFields.ContainsKey(af.Key)))
                {
                    var value = field.Value.GetValue(entity);
                    auditFields[field.Key].SetValue(auditEntity, value);
                }
            }
            return auditEntity;
        }

        private object CreateAuditEntityExplicit(Type auditType, EventEntry entry)
        {
            var auditEntity = Activator.CreateInstance(auditType);
            if (_ignoreMatchedPropertiesFunc == null || !_ignoreMatchedPropertiesFunc(auditType))
            {
                var auditFields = GetPropertiesToSet(auditType);
                foreach (var field in entry.ColumnValues.Where(cv => auditFields.ContainsKey(cv.Key)))
                {
                    auditFields[field.Key].SetValue(auditEntity, field.Value);
                }
            }
            return auditEntity;
        }

        private Dictionary<string, PropertyInfo> GetPropertiesToSet(Type type)
        {
            var result = new Dictionary<string, PropertyInfo>();
            foreach(var prop in type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetSetMethod() != null)
                .OrderBy(p => p.DeclaringType == type ? 1 : 0))
            {
                result[prop.Name] = prop;
            }
            return result;
        }

        private Dictionary<string, PropertyInfo> GetPropertiesToGet(Type type)
        {
            var result = new Dictionary<string, PropertyInfo>();
            foreach (var prop in type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetGetMethod() != null)
                .OrderBy(p => p.DeclaringType == type ? 1 : 0))
            {
                result[prop.Name] = prop;
            }
            return result;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            return;
        }

        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
