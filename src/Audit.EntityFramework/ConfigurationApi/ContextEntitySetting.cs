using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class ContextEntitySetting<TEntity> : IContextEntitySetting<TEntity>
    {
        internal HashSet<string> IgnoredProperties = new HashSet<string>();
        internal Dictionary<string, object> OverrideProperties = new Dictionary<string, object>();
        internal Dictionary<string, Func<object, object>> FormatProperties = new Dictionary<string, Func<object, object>>();

        public IContextEntitySetting<TEntity> Format<TProp>(Expression<Func<TEntity, TProp>> property, Func<TProp, TProp> format)
        {
            var name = GetMemberName(property);
            FormatProperties[name] = entity => format.Invoke((TProp)entity);
            return this;
        }

        public IContextEntitySetting<TEntity> Format<TProp>(Expression<Func<TEntity, TProp>> property, Func<TProp, object> format)
        {
            var name = GetMemberName(property);
            FormatProperties[name] = entity => format.Invoke((TProp)entity);
            return this;
        }

        public IContextEntitySetting<TEntity> Format<TProp>(string propertyName, Func<TProp, TProp> format)
        {
            FormatProperties[propertyName] = entity => format.Invoke((TProp)entity);
            return this;
        }

        public IContextEntitySetting<TEntity> Format<TProp>(string propertyName, Func<TProp, object> format)
        {
            FormatProperties[propertyName] = entity => format.Invoke((TProp)entity);
            return this;
        }

        public IContextEntitySetting<TEntity> Override<TProp>(Expression<Func<TEntity, TProp>> property, TProp value)
        {
            var name = GetMemberName(property);
            OverrideProperties[name] = value;
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

        public IContextEntitySetting<TEntity> Override<TProp>(string propertyName, TProp value)
        {
            OverrideProperties[propertyName] = value;
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
 