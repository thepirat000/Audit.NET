using System;
using System.Reflection;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class IncludeEntityConfigurator<TContext> : IIncludeEntityConfigurator<TContext>
    {
        public IIncludeEntityConfigurator<TContext> Include(Type entityType)
        {
            Configuration.IncludeEntity<TContext>(entityType);
            return this;
        }

        public IIncludeEntityConfigurator<TContext> Include<TEntity>(Action<IIncludePropertyConfigurator<TEntity>> includePropertiesConfiguration = null)
        {
            Configuration.IncludeEntity<TContext>(typeof(TEntity));

            if (includePropertiesConfiguration != null)
            {
                var configurator = new IncludePropertyConfigurator<TEntity>();
                includePropertiesConfiguration.Invoke(configurator);
                Configuration.IncludedProperties<TContext, TEntity>(configurator.IncludedPropertyNames);
            }

            return this;
        }
        public IIncludeEntityConfigurator<TContext> IncludeAny(Func<Type, bool> predicate)
        {
            Configuration.IncludedEntitiesFilter<TContext>(predicate);
            return this;
        }
    }
}