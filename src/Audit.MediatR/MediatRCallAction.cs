using Audit.Core;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Audit.MediatR;

/// <summary>
/// <para>Represents the audited details of a MediatR call (request, response, stream, and exception info).</para>
/// <para>This is the payload stored within the audit event for MediatR behaviors.</para>
/// </summary>
/// <remarks>
/// <para>Populated by <see cref="AuditMediatRBehavior{TRequest, TResponse}"/> and <see cref="AuditMediatRStreamBehavior{TRequest, TResponse}"/>.</para>
/// </remarks>
public class MediatRCallAction : IAuditOutput
{
    /// <summary>
    /// The MediatR call type as a string. <c>Request</c> or <c>StreamRequest</c>.
    /// </summary>
    public string CallType { get; set; }

    /// <summary>
    /// The name of the request type.
    /// </summary>
    public string RequestType { get; set; }

    /// <summary>
    /// The request payload captured for auditing, when enabled via options.
    /// May be <see langword="null"/> if request capture is disabled.
    /// </summary>
    public object Request { get; set; }

    /// <summary>
    /// The name of the response type (or stream item type for streaming calls).
    /// </summary>
    public string ResponseType { get; set; }

    /// <summary>
    /// The response payload captured for auditing on non-stream calls, when enabled via options.
    /// May be <see langword="null"/> if response capture is disabled or when using stream behaviors.
    /// </summary>
    public object Response { get; set; }

    /// <summary>
    /// The list of streamed response items captured for auditing on stream calls.
    /// Empty when the call is not a stream or when capture is disabled.
    /// </summary>
    public List<object> ResponseStream { get; set; }

    /// <summary>
    /// The exception information (string) when the call fails; otherwise <see langword="null"/>.
    /// </summary>
    public string Exception { get; set; }

    /// <summary>
    /// Indicates whether the call completed successfully (no exception recorded).
    /// </summary>
    public bool IsSuccess => Exception == null;

    /// <summary>
    /// Extension fields to include custom name/value pairs in the output.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> CustomFields { get; set; }

    [JsonIgnore]
    internal MediatRCallContext CallContext { get; set; }

    /// <summary>
    /// Gets the MediatR call context related to this action.
    /// </summary>
    public MediatRCallContext GetCallContext()
    {
        return CallContext;
    }

    /// <summary>
    /// Serializes this action to a JSON string using the configured audit JSON adapter.
    /// </summary>
    /// <returns>The JSON representation of this call action.</returns>
    public string ToJson()
    {
        return Configuration.JsonAdapter.Serialize(this);
    }
}