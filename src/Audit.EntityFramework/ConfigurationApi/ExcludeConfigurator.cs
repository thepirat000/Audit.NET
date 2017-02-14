using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class ExcludeConfigurator<T> : IExcludeConfigurator<T>
        where T : IAuditDbContext
    {
        public IExcludeConfigurator<T> Ignore(Type entityType)
        {
            Configuration.IgnoreEntity<T>(entityType);
            return this;
        }

        public IExcludeConfigurator<T> Ignore<TEntity>()
        {
            Configuration.IgnoreEntity<T>(typeof(TEntity));
            return this;
        }

        public IExcludeConfigurator<T> IgnoreAny(Func<Type, bool> predicate)
        {
            Configuration.IgnoredEntitiesFilter<T>(predicate);
            return this;
        }

    }
}