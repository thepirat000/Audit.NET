using Newtonsoft.Json;

namespace Audit.EntityFramework
{
    public class EventEntryChange
    {
        [JsonProperty(Order = 10)]
        public string ColumnName { get; set; }
        [JsonProperty(Order = 20)]
        public object OriginalValue { get; set; }
        [JsonProperty(Order = 30)]
        public object NewValue { get; set; }
    }
}