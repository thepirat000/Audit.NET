#if EF_FULL
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Represents an Independ Association for many-to-many relationships without a relationship entity
    /// </summary>
    public class AssociationEntry
    {
        [JsonProperty(Order = 10)]
        public string Table { get; set; }
        [JsonProperty(Order = 20)]
        public string Action { get; set; }
        [JsonProperty(Order = 30)]
        public AssociationEntryRecord[] Records { get; set; }
    }
}
#endif
