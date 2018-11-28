using Newtonsoft.Json;
using System.Collections.Generic;
using Audit.Core;
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
using Microsoft.EntityFrameworkCore;
#elif NET45
using System.Data.Entity;
#endif
namespace Audit.EntityFramework
{
    public class EntityFrameworkEvent : IAuditOutput
    {
        [JsonProperty(Order = 1)]
        public string Database { get; set; }
        [JsonProperty(Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public string ConnectionId { get; set; }
        [JsonProperty(Order = 4, NullValueHandling = NullValueHandling.Ignore)]
        public string AmbientTransactionId { get; set; }
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

        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Returns the DbContext associated to this event
        /// </summary>
        public DbContext GetDbContext()
        {
            return DbContext;
        }

        /// <summary>
        /// Serializes this Entity Framework event as a JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Audit.Core.Configuration.JsonSettings);
        }
        /// <summary>
        /// Parses an Entity Framework event from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Framework event representation.</param>
        public static EntityFrameworkEvent FromJson(string json)
        {
            return JsonConvert.DeserializeObject<EntityFrameworkEvent>(json, Audit.Core.Configuration.JsonSettings);
        }

    }
}