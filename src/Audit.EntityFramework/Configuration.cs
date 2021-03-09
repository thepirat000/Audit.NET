using Audit.EntityFramework.ConfigurationApi;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Global configuration for Audit.EntityFramework
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
        
        internal static void SetContextEntitySetting<T, TEntity>(Action<IContextEntitySetting<TEntity>> config)
            
        {
            var entitySettings = new ContextEntitySetting<TEntity>();
            config.Invoke(entitySettings);
            var entityConfig = EnsureConfigForEntity<T, TEntity>();
            entityConfig.IgnoredProperties = entitySettings.IgnoredProperties;
            entityConfig.OverrideProperties = entitySettings.OverrideProperties;
            entityConfig.FormatProperties = entitySettings.FormatProperties;
        }

        internal static void SetAuditEventType<T>(string eventType)
        {
            EnsureConfigFor<T>().AuditEventType = eventType;
        }

        internal static void SetIncludeEntityObjects<T>(bool include)
        {
            EnsureConfigFor<T>().IncludeEntityObjects = include;
        }

        internal static void SetExcludeValidationResults<T>(bool exclude)
        {
            EnsureConfigFor<T>().ExcludeValidationResults = exclude;
        }

        internal static void SetExcludeTransactionId<T>(bool exclude)
        {
            EnsureConfigFor<T>().ExcludeTransactionId = exclude;
        }

        internal static void SetEarlySavingAudit<T>(bool earlySaving)
        {
            EnsureConfigFor<T>().EarlySavingAudit = earlySaving;
        }

#if EF_FULL
        internal static void SetIncludeIndependantAssociations<T>(bool include)
        {
            EnsureConfigFor<T>().IncludeIndependantAssociations = include;
        }
#endif
        internal static void SetMode<T>(AuditOptionMode mode)
        {
            EnsureConfigFor<T>().Mode = mode;
        }

        internal static void IncludeEntity<TContext>(Type entityType)
        {
            EnsureConfigFor<TContext>().IncludedTypes.Add(entityType);
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

        internal static void Reset<T>()
        {
            _currentConfig.TryRemove(typeof(T), out _);
        }

        internal static EfSettings EnsureConfigFor<T>()
        {
            var t = typeof(T);
            EfSettings config;
            if (_currentConfig.TryGetValue(t, out config))
            {
                return config;
            }
            _currentConfig[t] = new EfSettings();
            return _currentConfig[t];
        }

        internal static EfEntitySettings EnsureConfigForEntity<T, TEntity>()
        {
            var tEntity = typeof(TEntity);
            var efSettings = EnsureConfigFor<T>();
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
