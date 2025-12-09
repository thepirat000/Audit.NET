using System;
using Audit.Core;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Hangfire.Server;

namespace Audit.Hangfire
{
    /// <summary>
    /// Data captured for Hangfire job execution and emitted by Audit.NET.
    /// Includes job id, type/method, server id, arguments, result and exception data.
    /// </summary>
    public class HangfireJobExecutionEvent : IAuditOutput
    {
        /// <summary>
        /// The Hangfire job identifier.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Type name of the job target.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Method description (as provided by Hangfire).
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Server identifier that executed the job.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// True when no exception was captured; otherwise false.
        /// </summary>
        public bool IsSuccess => Exception == null;

        /// <summary>
        /// Exception information captured during execution, if any.
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// Whether the job execution was canceled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Timestamp when the job was created (as recorded by Hangfire).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The result returned by the job method, if any.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Captured job arguments. May be null if excluded by configuration.
        /// </summary>
        public List<object> Args { get; set; }

        /// <summary>
        /// Arbitrary extension fields to include in the audit output.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new();

        /// <summary>
        /// Perform context captured for convenience; not serialized.
        /// </summary>
        [JsonIgnore]
        internal PerformContext Context { get; set; }

        /// <summary>
        /// Returns the underlying <see cref="PerformContext"/> associated with this event.
        /// </summary>
        public PerformContext GetPerformContext()
        {
            return Context;
        }

        /// <summary>
        /// Serializes the event to a JSON string using the configured Audit.NET JSON adapter.
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }
    }
}