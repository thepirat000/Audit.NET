using System;
using System.Collections.Generic;
using System.Linq.Expressions;
#if EF_CORE
using EntityEntry = Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry;
#else
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;
#endif

namespace Audit.EntityFramework.ConfigurationApi
{
    public class ContextEntitySetting<TEntity> : IContextEntitySetting<TEntity>
    {
        internal HashSet<string> IgnoredProperties = new HashSet<string>();
        internal Dictionary<string, Func<EntityEntry, object>> OverrideProperties = new Dictionary<string, Func<EntityEntry, object>>();
        internal Dictionary<string, Func<object, object>> FormatProperties = new Dictionary<string, Func<object, object>>();

        public IContextEntitySetting<TEntity> Format<TProp>(Expression<Func<TEntity, TProp>> property, Func<TProp, object> format)
        {
            var name = GetMemberName(property);
            FormatProperties[name] = value => format.Invoke((TProp)value);
            return this;
        }

        public IContextEntitySetting<TEntity> Format<TProp>(string propertyName, Func<TProp, object> format)
        {
            FormatProperties[propertyName] = value => format.Invoke((TProp)value);
            return this;
        }

        public IContextEntitySetting<TEntity> Override<TProp>(Expression<Func<TEntity, TProp>> property, Func<EntityEntry, object> valueSelector)
        {
            var name = GetMemberName(property);
            OverrideProperties[name] = entry => valueSelector?.Invoke(entry);
            return this;
        }
        
        public IContextEntitySetting<TEntity> Override(string propertyName, Func<EntityEntry, object> valueSelector)
        {
            OverrideProperties[propertyName] = entry => valueSelector?.Invoke(entry);
            return this;
        }

        public IContextEntitySetting<TEntity> Override<TProp>(Expression<Func<TEntity, TProp>> property, object value)
        {
            var name = GetMemberName(property);
            OverrideProperties[name] = _ => value;
            return this;
        }

        public IContextEntitySetting<TEntity> Override(string propertyName, object value)
        {
            OverrideProperties[propertyName] = _ => value;
            return this;
        }

        public IContextEntitySetting<TEntity> Ignore<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            var name = GetMemberName(property);
            IgnoredProperties.Add(name);
            return this;
        }

        public IContextEntitySetting<TEntity> Ignore(string propertyName)
        {
            IgnoredProperties.Add(propertyName);
            return this;
        }

        private string GetMemberName<T, TS>(Expression<Func<T, TS>> expression)
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
}
 