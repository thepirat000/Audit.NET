using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.EntityFramework.ConfigurationApi
{
    public interface IEntityFrameworkProviderConfiguratorExtra
    {
        /// <summary>
        /// Avoids the property matching from the entity to the audited entity. 
        /// </summary>
        /// <param name="ignore">Set to true to avoid copying the property values from the entity to the audited entity</param>
        void IgnoreMatchedProperties(bool ignore = true);
        /// <summary>
        /// Sets a function that determines if the property values must be copied for a specific audited entity type
        /// </summary>
        /// <param name="ignoreFunc">Function that receives the audited entity class type and returns true to ignore the automatic property matching.</param>
        void IgnoreMatchedProperties(Func<Type, bool> ignoreFunc);
    }
}
