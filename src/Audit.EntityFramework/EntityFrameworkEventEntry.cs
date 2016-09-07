using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    public class EntityFrameworkEventEntry
    {
        [JsonProperty(Order = 10)]
        public string EntityType { get; set; }
        [JsonProperty(Order = 25)]
        public IDictionary<string, object> PrimaryKey { get; set; }
        [JsonProperty(Order = 20)]
        public string Action { get; set; }
        [JsonProperty(Order = 30, NullValueHandling = NullValueHandling.Ignore)]
        public object Entity { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public List<EntityFrameworkEventEntryChange> Changes { get; set; }
        [JsonProperty(Order = 50)]
        public bool Valid { get; set; }
        [JsonProperty(Order = 60, NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ValidationResults { get; set; }
    }
}