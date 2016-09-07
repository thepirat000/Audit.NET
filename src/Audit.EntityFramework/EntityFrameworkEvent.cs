using Newtonsoft.Json;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    public class EntityFrameworkEvent
    {
        [JsonProperty(Order = 1)]
        public string Database { get; set; }
        [JsonProperty(Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionId { get; set; }
        [JsonProperty(Order = 10)]
        public List<EventEntry> Entries { get; set; }
        [JsonProperty(Order = 20)]
        public int Result { get; set; }
        [JsonProperty(Order = 30)]
        public bool Success { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorMessage { get; set; }
    }
}