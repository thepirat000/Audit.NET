
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
        /// <summary>
        /// The database name
        /// </summary>
        public string Database { get; set; }
        /// <summary>
        /// A unique identifier for the database connection.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different database connection.
        /// </summary>
        public string ConnectionId { get; set; }
#if EF_CORE_3_OR_GREATER
        /// <summary>
        /// A unique identifier for the context instance and pool lease, if any.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different context instances.
        /// </summary>
        public string ContextId { get; set; }
#endif
        /// <summary>
        /// The ambient transaction identifier, if any.
        /// </summary>
        public string AmbientTransactionId { get; set; }
        /// <summary>
        /// The local transaction identifier, if any.
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// Collection of affected entities
        /// </summary>
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