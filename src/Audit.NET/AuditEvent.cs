using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Audit.Core
{
    /// <summary>
    /// Represents the output of the audit process
    /// </summary>
    public class AuditEvent : IAuditOutput
    {
        /// <summary>
        /// The enviroment information
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AuditEventEnvironment Environment { get; set; }

        /// <summary>
        /// Indicates the change type (i.e. CustomerOrder Update)
        /// </summary>
        [JsonProperty(Order = -999)]
        public string EventType { get; set; }

        /// <summary>
        /// The extension data. 
        /// This will be serialized as the keys being properties of the current object.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; }

        /// <summary>
        /// The tracked target.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AuditTarget Target { get; set; }

        /// <summary>
        /// Comments.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Comments { get; set; }

        /// <summary>
        /// The date then the event started
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The date then the event finished
        /// </summary>
        public DateTime? EndDate { get; set; }

        ///<summary>
        /// The duration of the operation in milliseconds.
        /// </summary>
        [JsonProperty]
        public int Duration { get; set; }

        /// <summary>
        /// Converts the event to its JSON representation using JSON.NET.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Configuration.JsonSettings);
        }

        /// <summary>
        /// Converts the event to its JSON representation using JSON.NET with the given serializer settings.
        /// </summary>
        /// <param name="settings">Serializer settings to use.</param>
        public string ToJson(JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(this, settings);
        }

        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET.
        /// </summary>
        /// <param name="json">JSON string with the audit event representation.</param>
        public static T FromJson<T>(string json) where T : AuditEvent
        {
            return JsonConvert.DeserializeObject<T>(json, Configuration.JsonSettings);
        }
        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET.
        /// </summary>
        /// <param name="json">JSON string with the audit event representation.</param>
        public static AuditEvent FromJson(string json) 
        {
            return JsonConvert.DeserializeObject<AuditEvent>(json, Configuration.JsonSettings);
        }

        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET with the given serializer settings.
        /// </summary>
        /// <param name="settings">Serializer settings to use.</param>
        /// <param name="json">JSON string with the audit event representation.</param>
        public static T FromJson<T>(string json, JsonSerializerSettings settings) where T : AuditEvent
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Parses an AuditEvent from its JSON string representation using JSON.NET with the given serializer settings.
        /// </summary>
        /// <param name="json">JSON string with the audit event representation.</param>
        /// <param name="settings">Serializer settings to use.</param>
        public static AuditEvent FromJson(string json, JsonSerializerSettings settings)
        {
            return JsonConvert.DeserializeObject<AuditEvent>(json, settings);
        }

    }
}
