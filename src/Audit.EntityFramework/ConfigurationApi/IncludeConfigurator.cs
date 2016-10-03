using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class IncludeConfigurator<T> : IIncludeConfigurator<T>
        where T : AuditDbContext
    {
        public IncludeConfigurator<T> Include(Type entityType)
        {
            Configuration.IncludeEntity<T>(entityType);
            return this;
        }
        public IncludeConfigurator<T> Include<TEntity>()
        {
            Configuration.IncludeEntity<T>(typeof(TEntity));
            return this;
        }
        public IncludeConfigurator<T> IncludeAny(Func<Type, bool> predicate)
        {
            Configuration.IncludedEntitiesFilter<T>(predicate);
            return this;
        }
    }
}