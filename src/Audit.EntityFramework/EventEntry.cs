using Audit.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Audit.EntityFramework
{
    public class EventEntry : IAuditOutput
    {
        [JsonProperty(Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string Schema { get; set; }
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
            return JsonConvert.SerializeObject(this, Core.Configuration.JsonSettings);
        }
        /// <summary>
        /// Parses an Event Entry from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Event Entry representation.</param>
        public static EventEntry FromJson(string json)
        {
            return JsonConvert.DeserializeObject<EventEntry>(json, Core.Configuration.JsonSettings);
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