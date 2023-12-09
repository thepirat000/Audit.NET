using System;
using System.Collections.Generic;
using Audit.Core;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Events;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace Audit.MongoClient
{
    public class MongoCommandEvent : IAuditOutput
    {
        /// <summary>
        /// The Request identifier.
        /// </summary>
        public int RequestId { get; set; }

        /// <summary>
        /// The Connection information.
        /// </summary>
        public MongoConnection Connection { get; set; }

        /// <summary>
        /// The Operation identifier.
        /// </summary>
        public long? OperationId { get; set; }

        /// <summary>
        /// The Command name.
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// The command body to be executed
        /// </summary>
        public object Body { get; set; }
        
        ///<summary>
        /// The duration of the Mongo Event in milliseconds.
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Indicates if the command succeeded.
        /// </summary>
        public bool? Success { get; set; }
                     
        /// <summary>
        /// The database reply.
        /// </summary>
#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public object Reply { get; set; }

        /// <summary>
        /// The database error message if an error occurred, otherwise NULL.
        /// </summary>
#if IS_NK_JSON
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#else
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
        public string Error { get; set; }

        /// <summary>
        /// The command event Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        [JsonExtensionData]
        [BsonExtraElements]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        [JsonIgnore]
        internal CommandStartedEvent CommandStartedEvent { get; set; }

        /// <summary>
        /// Returns the DbContext associated to this event
        /// </summary>
        public CommandStartedEvent GetCommandStartedEvent()
        {
            return CommandStartedEvent;
        }

        /// <summary>
        /// Serializes this Audit Mongo Command as a JSON string
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an Audit Mongo Command from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Mongo Command representation.</param>
        public static MongoCommandEvent FromJson(string json)
        {
            return Configuration.JsonAdapter.Deserialize<MongoCommandEvent>(json);
        }
    }
}