using System;
using System.Collections.Generic;

namespace Audit.Grpc.Client;

/// <summary>
/// Represents an audited gRPC client call action.
/// </summary>
public class GrpcClientCallAction
{
    /// <summary>
    /// Gets the type of the method. "Unary", "ClientStreaming", "ServerStreaming" or "DuplexStreaming".
    /// </summary>
    public string MethodType { get; set; }

    /// <summary>
    /// Gets the name of the service to which this method belongs.
    /// </summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// Gets the unqualified name of the method.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Fully qualified name of the method. 
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Host that the current invocation will be dispatched to.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Call deadline.
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Request headers metadata.
    /// </summary>
    public List<GrpcMetadata> RequestHeaders { get; set; }

    /// <summary>
    /// Response headers metadata.
    /// </summary>
    public List<GrpcMetadata> ResponseHeaders { get; set; }

    /// <summary>
    /// Call trailing metadata.
    /// </summary>
    public List<GrpcMetadata> Trailers { get; set; }

    /// <summary>
    /// Type of the request message.
    /// </summary>
    public string RequestType { get; set; }

    /// <summary>
    /// Type of the response message.
    /// </summary>
    public string ResponseType { get; set; }

    /// <summary>
    /// Request message
    /// </summary>
    public object Request { get; set; }

    /// <summary>
    /// Request stream messages in case of streaming calls.
    /// </summary>
    public List<object> RequestStream { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public object Response { get; set; }

    /// <summary>
    /// Response stream messages in case of streaming calls.
    /// </summary>
    public List<object> ResponseStream { get; set; }
    
    /// <summary>
    /// Exception message in case of failure.
    /// </summary>
    public string Exception { get; set; }

    /// <summary>
    /// Indicates whether the call was successful.
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Status code of the gRPC call.
    /// </summary>
    public string StatusCode { get; set; }

    /// <summary>
    /// Additional status detail of the gRPC call.
    /// </summary>
    public string StatusDetail { get; set; }
}