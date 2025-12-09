using Audit.Core;

using Hangfire.Client;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Audit.Hangfire
{
    /// <summary>
    /// Data captured for Hangfire job creation and emitted by Audit.NET.
    /// Includes job id, type/method, initial state, arguments/parameters, scheduling data and continuation info.
    /// </summary>
    public class HangfireJobCreationEvent : IAuditOutput
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
        /// Initial state name assigned at creation (e.g. Scheduled, Enqueued, Awaiting).
        /// </summary>
        public string InitialState { get; set; }

        /// <summary>
        /// True when no exception was captured; otherwise false.
        /// </summary>
        public bool IsSuccess => Exception == null;

        /// <summary>
        /// Exception information captured during creation, if any.
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// Whether the job creation was canceled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Timestamp when the job was created (as recorded by Hangfire).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// For scheduled jobs, the timestamp when the job was scheduled to run.
        /// </summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>
        /// For scheduled jobs, the timestamp when the job is planned to be enqueued.
        /// </summary>
        public DateTime? EnqueueAt { get; set; }

        /// <summary>
        /// For fire-and-forget jobs, the timestamp when the job was enqueued.
        /// </summary>
        public DateTime? EnqueuedAt { get; set; }

        /// <summary>
        /// Queue name used for enqueuing (when applicable).
        /// </summary>
        public string Queue { get; set; }

        /// <summary>
        /// Captured job arguments. May be null if excluded by configuration.
        /// </summary>
        public List<object> Args { get; set; }

        /// <summary>
        /// Captured job parameters (context.Parameters). May be null if not included by configuration.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Continuation metadata when the job is a continuation (parent id, options, expiration).
        /// </summary>
        public ContinuationData Continuation { get; set; }

        /// <summary>
        /// Arbitrary extension fields to include in the audit output.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new();

        /// <summary>
        /// Creation context captured for convenience; not serialized.
        /// </summary>
        [JsonIgnore]
        internal CreateContext Context { get; set; }

        /// <summary>
        /// Returns the underlying <see cref="CreateContext"/> associated with this event.
        /// </summary>
        public CreateContext GetCreateContext()
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