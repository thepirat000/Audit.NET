using System;
using System.Collections.Generic;

namespace Audit.EntityFramework.ConfigurationApi
{
    internal class EfSettings
    {
        internal EfSettings()
        {
            IncludedTypes = new HashSet<Type>();
            IgnoredTypes = new HashSet<Type>();
        }
        public string AuditEventType { get; set; }
        public bool? IncludeEntityObjects { get; set; }
        public AuditOptionMode? Mode { get; set; }
        public HashSet<Type> IncludedTypes { get; set; }
        public HashSet<Type> IgnoredTypes { get; set; }
        public Func<Type, bool> IgnoredTypesFilter { get; set; }
        public Func<Type, bool> IncludedTypesFilter { get; set; }
    }
}
