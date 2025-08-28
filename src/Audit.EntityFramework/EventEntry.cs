using Audit.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Event Entry class representing an entity change in the audit event.
    /// </summary>
    public class EventEntry : IAuditOutput
    {
        /// <summary>
        /// The schema of the table (if applicable)
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// The table name
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// The entity display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The primary key values for the entity
        /// </summary>
        public IDictionary<string, object> PrimaryKey { get; set; }

        /// <summary>
        /// The action performed (Insert, Update, Delete)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The entity object (if IncludeEntityObjects is set to true)
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// The list of changes made (for Update actions, if MapChangesByColumn is false)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<EventEntryChange> Changes { get; set; }

        /// <summary>
        /// The dictionary of changes made, indexed by column name (for Update actions, if MapChangesByColumn is true)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ColumnValueChange> ChangesByColumn { get; set; }

        /// <summary>
        /// The current values of the entity columns
        /// </summary>
        public IDictionary<string, object> ColumnValues { get; set; }

        /// <summary>
        /// To indicate if the entity is valid according to its validation rules.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// The list of validation results (if ExcludeValidationResults is set to false)
        /// </summary>
        public List<string> ValidationResults { get; set; }

        /// <summary>
        /// Custom fields added to the event entry
        /// </summary>
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

        /// <summary>
        /// The source entity type (not included on the output)
        /// </summary>
        [JsonIgnore]
        public Type EntityType { get; set; }

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