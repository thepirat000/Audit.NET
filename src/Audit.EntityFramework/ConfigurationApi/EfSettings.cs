using System;
using System.Collections.Generic;

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Internal representation of the settings related to one context type
    /// </summary>
    internal class EfSettings
    {
        internal EfSettings()
        {
            IncludedTypes = new HashSet<Type>();
            IgnoredTypes = new HashSet<Type>();
            EntitySettings = new Dictionary<Type, EfEntitySettings>();
        }
        public string AuditEventType { get; set; }
        public bool? IncludeEntityObjects { get; set; }
        public bool? ExcludeValidationResults { get; set; }
        public AuditOptionMode? Mode { get; set; }
        public HashSet<Type> IncludedTypes { get; set; }
        public HashSet<Type> IgnoredTypes { get; set; }
        public Func<Type, bool> IgnoredTypesFilter { get; set; }
        public Func<Type, bool> IncludedTypesFilter { get; set; }
        public Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        public bool? ExcludeTransactionId { get; set; }
#if EF_FULL
        public bool? IncludeIndependantAssociations { get; set; }
#endif
        public bool? ReloadDatabaseValues { get; set; }
    }
}
