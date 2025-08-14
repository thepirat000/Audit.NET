using Audit.EntityFramework.ConfigurationApi;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Global configuration for High-Level SaveChanges interceptor
    /// </summary>
    public static class Configuration
    {
        private static ConcurrentDictionary<Type, EfSettings> _currentConfig = new ConcurrentDictionary<Type, EfSettings>();

        /// <summary>
        /// Configure Audit EF Settings by using a Fluent Configuration API.
        /// </summary>
        public static IContextConfigurator Setup()
        {
            return new ContextConfigurator();
        }
        
        internal static void SetContextEntitySetting<TContext, TEntity>(Action<IContextEntitySetting<TEntity>> config)
        {
            var entitySettings = new ContextEntitySetting<TEntity>();
            config.Invoke(entitySettings);
            var entityConfig = EnsureConfigForEntity<TContext, TEntity>();
            entityConfig.IgnoredProperties = entitySettings.IgnoredProperties;
            entityConfig.OverrideProperties = entitySettings.OverrideProperties;
            entityConfig.FormatProperties = entitySettings.FormatProperties;
        }

        internal static void SetAuditEventType<TContext>(string eventType)
        {
            EnsureConfigFor<TContext>().AuditEventType = eventType;
        }

        internal static void SetIncludeEntityObjects<TContext>(bool include)
        {
            EnsureConfigFor<TContext>().IncludeEntityObjects = include;
        }

        internal static void SetExcludeValidationResults<TContext>(bool exclude)
        {
            EnsureConfigFor<TContext>().ExcludeValidationResults = exclude;
        }

        internal static void SetExcludeTransactionId<TContext>(bool exclude)
        {
            EnsureConfigFor<TContext>().ExcludeTransactionId = exclude;
        }

#if EF_FULL
        internal static void SetIncludeIndependantAssociations<TContext>(bool include)
        {
            EnsureConfigFor<TContext>().IncludeIndependantAssociations = include;
        }
#endif

        internal static void SetReloadDatabaseValues<TContext>(bool reloadDatabaseValues)
        {
            EnsureConfigFor<TContext>().ReloadDatabaseValues = reloadDatabaseValues;
        }

        internal static void SetMode<TContext>(AuditOptionMode mode)
        {
            EnsureConfigFor<TContext>().Mode = mode;
        }

        internal static void IncludeEntity<TContext>(Type entityType)
        {
            EnsureConfigFor<TContext>().IncludedTypes.Add(entityType);
        }

        internal static void IncludedProperties<TContext, TEntity>(HashSet<string> propertyNames)
        {
            var efConfig = EnsureConfigFor<TContext>();
            efConfig.IncludedPropertyNames ??= [];
            efConfig.IncludedPropertyNames[typeof(TEntity)] = propertyNames;
        }

        internal static void IgnoreEntity<TContext>(Type entityType)
        {
            EnsureConfigFor<TContext>().IgnoredTypes.Add(entityType);
        }

        internal static void IgnoredEntitiesFilter<TContext>(Func<Type, bool> predicate)
        {
            EnsureConfigFor<TContext>().IgnoredTypesFilter = predicate;
        }

        internal static void IncludedEntitiesFilter<TContext>(Func<Type, bool> predicate)
        {
            EnsureConfigFor<TContext>().IncludedTypesFilter = predicate;
        }

        internal static void Reset<TContext>()
        {
            _currentConfig.TryRemove(typeof(TContext), out _);
        }

        internal static EfSettings EnsureConfigFor<TContext>()
        {
            var t = typeof(TContext);
            EfSettings config;
            if (_currentConfig.TryGetValue(t, out config))
            {
                return config;
            }
            _currentConfig[t] = new EfSettings();
            return _currentConfig[t];
        }

        internal static EfEntitySettings EnsureConfigForEntity<TContext, TEntity>()
        {
            var tEntity = typeof(TEntity);
            var efSettings = EnsureConfigFor<TContext>();
            if (efSettings.EntitySettings.TryGetValue(tEntity, out EfEntitySettings value))
            {
                return value;
            }
            efSettings.EntitySettings[tEntity] = new EfEntitySettings();
            return efSettings.EntitySettings[tEntity];
        }

        internal static EfSettings GetConfigForType(Type contextType)
        {
            EfSettings result;
            if (_currentConfig.TryGetValue(contextType, out result))
            {
                return result;
            }
            return null;
        }
    }
}
