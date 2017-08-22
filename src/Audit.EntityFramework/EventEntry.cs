using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    public class EventEntry
    {
        [JsonProperty(Order = 10)]
        public string Table { get; set; }
        [JsonProperty(Order = 25)]
        public IDictionary<string, object> PrimaryKey { get; set; }
        [JsonProperty(Order = 20)]
        public string Action { get; set; }
        [JsonProperty(Order = 30, NullValueHandling = NullValueHandling.Ignore)]
        public object Entity { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public List<EventEntryChange> Changes { get; set; }
        [JsonProperty(Order = 45, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> ColumnValues { get; set; }
        [JsonProperty(Order = 50)]
        public bool Valid { get; set; }
        [JsonProperty(Order = 60, NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ValidationResults { get; set; }

        [JsonIgnore]
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
        internal Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; set; }
#else
        internal System.Data.Entity.Infrastructure.DbEntityEntry Entry { get; set; }
#endif
    }
}