using Audit.Core;
using System;
using System.Collections.Generic;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.EntityFramework
{
    public class EventEntry : IAuditOutput
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Name { get; set; }
        public IDictionary<string, object> PrimaryKey { get; set; }
        public string Action { get; set; }
        public object Entity { get; set; }
        public List<EventEntryChange> Changes { get; set; }
        public IDictionary<string, object> ColumnValues { get; set; }
        public bool Valid { get; set; }
        public List<string> ValidationResults { get; set; }

        /// <summary>
        /// The source entity type (not included on the output)
        /// </summary>
        [JsonIgnore]
        public Type EntityType { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serializes this Event Entry as a JSON string
        /// </summary>
        public string ToJson()
        {
            return Core.Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an Event Entry from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Event Entry representation.</param>
        public static EventEntry FromJson(string json)
        {
            return Core.Configuration.JsonAdapter.Deserialize<EventEntry>(json);
        }

        [JsonIgnore]
#if EF_CORE
        internal Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; set; }
        /// <summary>
        /// Returns the EntityEntry associated to this audit event entry
        /// </summary>
        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry GetEntry()
        {
            return Entry;
        }
#else
        internal System.Data.Entity.Infrastructure.DbEntityEntry Entry { get; set; }

        /// <summary>
        /// Returns the DbEntityEntry associated to this audit event entry
        /// </summary>
        public System.Data.Entity.Infrastructure.DbEntityEntry GetEntry()
        {
            return Entry;
        }
#endif
    }
}