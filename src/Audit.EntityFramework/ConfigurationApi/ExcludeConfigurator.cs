using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class ExcludeConfigurator<T> : IExcludeConfigurator<T>
        where T : AuditDbContext
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
    }
}