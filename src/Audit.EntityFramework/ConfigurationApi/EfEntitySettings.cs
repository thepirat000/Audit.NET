using System;
using System.Collections.Generic;
#if EF_CORE
using EntityEntry = Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry;
#else
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;
#endif

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Representation of the settings related to one entity type in a context
    /// </summary>
    public class EfEntitySettings
    {
        /// <summary>
        /// To indicate the entity's properties (columns) to be ignored on the audit logs. Key: property name.
        /// </summary>
        public HashSet<string> IgnoredProperties = new HashSet<string>();
        /// <summary>
        /// Override callbacks for properties on the audit logs. Key: property name.
        /// Value: a function of the entity entry and <see cref="PropertyOverrideContext"/> that returns the value to store.
        /// </summary>
        public Dictionary<string, Func<EntityEntry, PropertyOverrideContext, object>> OverrideProperties = new Dictionary<string, Func<EntityEntry, PropertyOverrideContext, object>>();
        /// <summary>
        /// To indicate replacement functions for the property's values on the audit logs. Key: property name, Value: function of the actual value.
        /// </summary>
        public Dictionary<string, Func<object, object>> FormatProperties = new Dictionary<string, Func<object, object>>();

    }
}
