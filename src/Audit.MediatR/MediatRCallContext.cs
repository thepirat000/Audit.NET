using System;

namespace Audit.MediatR;

/// <summary>
/// <para>Lightweight context describing a MediatR call being audited.</para>
/// <para>Includes the call type, the request instance (when available), and the request/response CLR types.</para>
/// </summary>
/// <remarks>
/// <para>Used by auditing behaviors to drive filters and payload inclusion decisions.</para>
/// </remarks>
public struct MediatRCallContext
{
    /// <summary>
    /// The MediatR call type (e.g., <see cref="MediatRCallType.Request"/> or <see cref="MediatRCallType.StreamRequest"/>).
    /// </summary>
    public MediatRCallType CallType { get; set; }

    /// <summary>
    /// The request instance associated with the call. May be <see langword="null"/> if not supplied or capture is disabled.
    /// </summary>
    public object Request { get; set; }

    /// <summary>
    /// The CLR type of the request.
    /// </summary>
    public Type RequestType { get; set; }

    /// <summary>
    /// The CLR type of the response (or stream item type for streaming calls).
    /// </summary>
    public Type ResponseType { get; set; }

    /// <summary>
    /// Initializes a new empty context. Properties should be set explicitly after construction.
    /// </summary>
    public MediatRCallContext()
    {
    }

    /// <summary>
    /// Initializes a new context with the provided call type, request instance, and request/response types.
    /// </summary>
    /// <param name="callType">The kind of MediatR call being audited.</param>
    /// <param name="request">The request instance (optional).</param>
    /// <param name="requestType">The CLR type of the request.</param>
    /// <param name="responseType">The CLR type of the response or stream item.</param>
    public MediatRCallContext(MediatRCallType callType, object request, Type requestType, Type responseType)
    {
        CallType = callType;
        Request = request;
        RequestType = requestType;
        ResponseType = responseType;
    }
}