#if NET45
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Audit.EntityFramework
{
    public class AssociationEntryRecord
    {
        [JsonProperty(Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string Schema { get; set; }
        [JsonProperty(Order = 10)]
        public string Table { get; set; }
        [JsonProperty(Order = 20, NullValueHandling = NullValueHandling.Ignore)]
        public object Entity { get; set; }
        [JsonProperty(Order = 30)]
        public IDictionary<string, object> PrimaryKey { get; set; }
        [JsonIgnore]
        internal object InternalEntity { get; set; }
    }
}
#endif