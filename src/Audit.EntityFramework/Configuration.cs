using Audit.EntityFramework.ConfigurationApi;
using System;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Global configuration for Audit.EntityFramework
    /// </summary>
    public static class Configuration
    {
        private static Dictionary<Type, EfSettings> _currentConfig = new Dictionary<Type, EfSettings>();

        /// <summary>
        /// Configure Audit EF Settings by using a Fluent Configuration API.
        /// </summary>
        public static IContextConfigurator Setup()
        {
            return new ContextConfigurator();
        }

        internal static void SetAuditEventType<T>(string eventType)
            where T : IAuditDbContext
        {
            EnsureConfigFor<T>().AuditEventType = eventType;
        }

        internal static void SetIncludeEntityObjects<T>(bool include)
            where T : IAuditDbContext
        {
            EnsureConfigFor<T>().IncludeEntityObjects = include;
        }

        internal static void SetMode<T>(AuditOptionMode mode)
            where T : IAuditDbContext
        {
            EnsureConfigFor<T>().Mode = mode;
        }

        internal static void IncludeEntity<TContext>(Type entityType)
            where TContext : IAuditDbContext
        {
            EnsureConfigFor<TContext>().IncludedTypes.Add(entityType);
        }

        internal static void IgnoreEntity<TContext>(Type entityType)
            where TContext : IAuditDbContext
        {
            EnsureConfigFor<TContext>().IgnoredTypes.Add(entityType);
        }

        internal static void IgnoredEntitiesFilter<TContext>(Func<Type, bool> predicate)
            where TContext : IAuditDbContext
        {
            EnsureConfigFor<TContext>().IgnoredTypesFilter = predicate;
        }

        internal static void IncludedEntitiesFilter<TContext>(Func<Type, bool> predicate)
            where TContext : IAuditDbContext
        {
            EnsureConfigFor<TContext>().IncludedTypesFilter = predicate;
        }

        internal static void Reset<T>()
            where T : IAuditDbContext
        {
            _currentConfig.Remove(typeof(T));
        }

        internal static EfSettings EnsureConfigFor<T>()
            where T : IAuditDbContext
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
