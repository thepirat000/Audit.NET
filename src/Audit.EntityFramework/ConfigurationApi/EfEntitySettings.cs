using System;
using System.Collections.Generic;

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
        /// To indicate constant values to override properties on the audit logs. Key: property name, Value: constant value.
        /// </summary>
        public Dictionary<string, object> OverrideProperties = new Dictionary<string, object>();
        /// <summary>
        /// To indicate replacement functions for the property's values on the audit logs. Key: property name, Value: function of the actual value.
        /// </summary>
        public Dictionary<string, Func<object, object>> FormatProperties = new Dictionary<string, Func<object, object>>();

    }
}
