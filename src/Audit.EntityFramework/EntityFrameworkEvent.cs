
using System.Collections.Generic;
using Audit.Core;
#if EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.EntityFramework
{
    public class EntityFrameworkEvent : IAuditOutput
    {
        public string Database { get; set; }
        public string ConnectionId { get; set; }
        public string AmbientTransactionId { get; set; }
        public string TransactionId { get; set; }
        public List<EventEntry> Entries { get; set; }
#if EF_FULL
        public List<AssociationEntry> Associations { get; set; }
#endif
        public int Result { get; set; }
        public bool Success { get; set; }
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
            return Core.Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an Entity Framework event from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Framework event representation.</param>
        public static EntityFrameworkEvent FromJson(string json)
        {
            return Core.Configuration.JsonAdapter.Deserialize<EntityFrameworkEvent>(json);
        }
    }
}