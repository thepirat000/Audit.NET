using System.Linq;
using Audit.Core;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Audit.EntityFramework.ConfigurationApi;
using System.Threading;
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
        private bool _disposeDbContext;
        private Func<EventEntry, Type> _explicitMapper;
        private Func<DbContext, EventEntry, object> _auditEntityCreator;

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
                _auditEntityCreator = efConfig._auditEntityCreator;
                _disposeDbContext = efConfig._disposeDbContext;
            }
        }

        /// <summary>
        /// A function that creates a new audit entity instance from the Event Entry and the Audit DbContext. 
        /// </summary>
        public Func<DbContext, EventEntry, object> AuditEntityCreator
        {
            get { return _auditEntityCreator; }
            set { _auditEntityCreator = value; }
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

        /// <summary>
        /// Indicates if the Audit DbContext should be disposed after saving the audit
        /// </summary>
        public bool DisposeDbContext
        {
            get { return _disposeDbContext; }
            set { _disposeDbContext = value; }
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
            try
            {
                foreach (var entry in efEvent.EntityFrameworkEvent.Entries)
                {
                    Type entityType = GetEntityType(entry, localDbContext);
                    entry.EntityType = entityType;
                    // Explicit creator (Entry -> object)
                    object auditEntity = CreateAuditEntityFromFactory(entityType, entry, auditDbContext);
                    Type mappedType = GetTypeNoProxy(auditEntity?.GetType());
                    if (auditEntity == null)
                    {
                        mappedType = _explicitMapper?.Invoke(entry);
                        if (mappedType != null)
                        {
                            // Explicit mapping (Entry -> Type)
                            auditEntity = CreateAuditEntityFromType(entityType, mappedType, entry);
                        }
                        else
                        {
                            // Implicit mapping (Type -> Type)
                            if (entityType != null)
                            {
                                mappedType = _auditTypeMapper?.Invoke(entityType, entry);
                                if (mappedType != null)
                                {
                                    auditEntity = CreateAuditEntityFromType(entityType, mappedType, entry);
                                }
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
                    if (auditDbContext is IAuditBypass bypass)
                    {
                        bypass.SaveChangesBypassAudit();
                    }
                    else
                    {
                        auditDbContext.SaveChanges();
                    }
                }
            }
            finally
            {
                if (_disposeDbContext)
                {
                    auditDbContext.Dispose();
                }
            }

            return auditEvent;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            bool save = false;
            if (!(auditEvent is AuditEventEntityFramework efEvent))
            {
                return null;
            }
            var localDbContext = efEvent.EntityFrameworkEvent.DbContext;
            var auditDbContext = DbContextBuilder?.Invoke(efEvent) ?? localDbContext;
            try
            {
                foreach (var entry in efEvent.EntityFrameworkEvent.Entries)
                {
                    Type entityType = GetEntityType(entry, localDbContext);
                    entry.EntityType = entityType;
                    // Explicit creator (Entry -> object)
                    object auditEntity = CreateAuditEntityFromFactory(entityType, entry, auditDbContext);
                    Type mappedType = GetTypeNoProxy(auditEntity?.GetType());
                    if (auditEntity == null)
                    {
                        mappedType = _explicitMapper?.Invoke(entry);
                        if (mappedType != null)
                        {
                            // Explicit mapping (Entry -> Type)
                            auditEntity = CreateAuditEntityFromType(entityType, mappedType, entry);
                        }
                        else
                        {
                            // Implicit mapping (Type -> Type)
                            if (entityType != null)
                            {
                                mappedType = _auditTypeMapper?.Invoke(entityType, entry);
                                if (mappedType != null)
                                {
                                    auditEntity = CreateAuditEntityFromType(entityType, mappedType, entry);
                                }
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
                            await auditDbContext.AddAsync(auditEntity, cancellationToken);
#endif
                            save = true;
                        }
                    }
                }
                if (save)
                {
                    if (auditDbContext is IAuditBypass bypass)
                    {
                        await bypass.SaveChangesBypassAuditAsync(cancellationToken);
                    }
                    else
                    {
                        await auditDbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            finally
            {
                if (_disposeDbContext)
                {
#if EF_CORE_1 || EF_CORE_2 || EF_FULL
                    auditDbContext.Dispose();
#else
                    await auditDbContext.DisposeAsync();
#endif
                }
            }

            return auditEvent;
        }

        private Type GetEntityType(EventEntry entry, DbContext localDbContext)
        {
            var entryType = GetTypeNoProxy(entry.Entry.Entity.GetType());
            Type type;
#if EF_CORE_6_OR_GREATER
            IReadOnlyEntityType definingType = entry.Entry.Metadata.FindOwnership()?.DeclaringEntityType ?? localDbContext.Model.FindEntityType(entry.Entry.Metadata.Name);
            type = definingType?.ClrType;
#elif EF_CORE_3_OR_GREATER
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

        private object CreateAuditEntityFromType(Type definingType, Type auditType, EventEntry entry)
        {
            var auditEntity = Activator.CreateInstance(auditType);
            if (_ignoreMatchedPropertiesFunc == null || !_ignoreMatchedPropertiesFunc(auditType))
            {
                SetAuditEntityMatchedProperties(definingType, entry, auditType, auditEntity);
            }
            return auditEntity;
        }

        private object CreateAuditEntityFromFactory(Type definingType, EventEntry entry, DbContext auditDbContext)
        {
            var auditEntity = _auditEntityCreator?.Invoke(auditDbContext, entry);
            if (auditEntity == null)
            {
                return null;
            }
            var auditType = GetTypeNoProxy(auditEntity.GetType());
            if (_ignoreMatchedPropertiesFunc == null || !_ignoreMatchedPropertiesFunc(auditType))
            {
                SetAuditEntityMatchedProperties(definingType, entry, auditType, auditEntity);
            }
            return auditEntity;
        }

        private void SetAuditEntityMatchedProperties(Type definingType, EventEntry entry, Type auditType, object auditEntity)
        {
            var entity = entry.Entry.Entity;
            var auditFields = GetPropertiesToSet(auditType);
#if EF_CORE
            var entityFields = entry.GetEntry().Metadata.GetProperties();
            foreach (var prop in entityFields.Where(af => auditFields.ContainsKey(af.Name)))
            {
                var colName = DbContextHelper.GetColumnName(prop);
                var value = entry.ColumnValues.ContainsKey(colName) ? entry.ColumnValues[colName] : prop.PropertyInfo?.GetValue(entity);
                auditFields[prop.Name].SetValue(auditEntity, value);
            }
#else
            var entityFields = GetPropertiesToGet(definingType);
            foreach (var prop in entityFields.Where(af => auditFields.ContainsKey(af.Key)))
            {
                var value = entry.ColumnValues.ContainsKey(prop.Key) ? entry.ColumnValues[prop.Key] : prop.Value.GetValue(entity);
                auditFields[prop.Key].SetValue(auditEntity, value);
            }
#endif
        }

        private Dictionary<string, PropertyInfo> GetPropertiesToSet(Type type)
        {
            var result = new Dictionary<string, PropertyInfo>();
            foreach (var prop in type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetSetMethod() != null)
                .OrderBy(p => p.DeclaringType == type ? 1 : 0))
            {
                result[prop.Name] = prop;
            }
            return result;
        }

#if EF_FULL
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
#endif
        private Type GetTypeNoProxy(Type type)
        {
            if (type == null)
            {
                return null;
            }
            if (type.FullName.StartsWith("Castle.Proxies."))
            {
                return type.GetTypeInfo().BaseType;
            }
            return type;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            return;
        }

        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
