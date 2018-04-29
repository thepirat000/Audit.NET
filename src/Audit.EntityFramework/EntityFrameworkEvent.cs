using Newtonsoft.Json;
using System.Collections.Generic;
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
using Microsoft.EntityFrameworkCore;
#elif NET45
using System.Data.Entity;
#endif
namespace Audit.EntityFramework
{
    public class EntityFrameworkEvent
    {
        [JsonProperty(Order = 1)]
        public string Database { get; set; }
        [JsonProperty(Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public string ConnectionId { get; set; }
        [JsonProperty(Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionId { get; set; }
        [JsonProperty(Order = 10)]
        public List<EventEntry> Entries { get; set; }
#if NET45
        [JsonProperty(Order = 15, NullValueHandling = NullValueHandling.Ignore)]
        public List<AssociationEntry> Associations { get; set; }
#endif
        [JsonProperty(Order = 20)]
        public int Result { get; set; }
        [JsonProperty(Order = 30)]
        public bool Success { get; set; }
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorMessage { get; set; }
        [JsonIgnore]
        internal DbContext DbContext { get; set; }

        /// <summary>
        /// Returns the DbContext associated to this event
        /// </summary>
        public DbContext GetDbContext()
        {
            return DbContext;
        }
    }
}