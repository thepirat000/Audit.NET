using System;
using System.Linq.Expressions;

namespace Audit.EntityFramework.ConfigurationApi;

public interface IIncludePropertyConfigurator<TEntity>
{
    IncludePropertyConfigurator<TEntity> IncludeProperty<TProp>(Expression<Func<TEntity, TProp>> propertySelector);
    IncludePropertyConfigurator<TEntity> IncludeProperty(string propertyName);
}