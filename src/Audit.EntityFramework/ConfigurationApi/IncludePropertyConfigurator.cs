using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Audit.EntityFramework.ConfigurationApi;

public class IncludePropertyConfigurator<TEntity> : IIncludePropertyConfigurator<TEntity>
{
    internal HashSet<string> IncludedPropertyNames = [];

    public IncludePropertyConfigurator<TEntity> IncludeProperty<TProp>(Expression<Func<TEntity, TProp>> propertySelector)
    {
        var propertyName = GetMemberName(propertySelector);
        IncludedPropertyNames.Add(propertyName);
        return this;
    }

    public IncludePropertyConfigurator<TEntity> IncludeProperty(string propertyName)
    {
        IncludedPropertyNames.Add(propertyName);
        return this;
    }

    private static string GetMemberName<T, TS>(Expression<Func<T, TS>> expression)
    {
        if (!(expression.Body is MemberExpression me))
        {
            throw new ArgumentException("The expression is not a member expression", nameof(expression));
        }
        if (!(me.Expression is ParameterExpression))
        {
            throw new ArgumentException("The body expression is not a parameter expression", nameof(expression));
        }
        return me.Member.Name;
    }
}