#if EF_FULL
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Represents an Independ Association for many-to-many relationships without a relationship entity
    /// </summary>
    public class AssociationEntry
    {
        public string Table { get; set; }
        public string Action { get; set; }
        public AssociationEntryRecord[] Records { get; set; }
    }
}
#endif
