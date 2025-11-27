using Audit.Core;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Grpc.Core;

namespace Audit.Grpc.Server;


/// <summary>
/// Represents an audited gRPC server call action.
/// </summary>
public class GrpcServerCallAction : IAuditOutput
{
    /// <summary>
    /// Gets the type of the method. "Unary", "ClientStreaming", "ServerStreaming" or "DuplexStreaming".
    /// </summary>
    public string MethodType { get; set; }

    /// <summary>
    /// Gets the unqualified name of the method.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Call deadline.
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Request headers metadata.
    /// </summary>
    public List<GrpcMetadata> RequestHeaders { get; set; }

    /// <summary>
    /// Call trailing metadata.
    /// </summary>
    public List<GrpcMetadata> Trailers { get; set; }
    
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

    public string Peer { get; set; }
    public string Host { get; set; }
    public object Request { get; set; }
    public object Response { get; set; }

    /// <summary>
    /// Request stream messages in case of streaming calls.
    /// </summary>
    public List<object> RequestStream { get; set; }

    /// <summary>
    /// Response stream messages in case of streaming calls.
    /// </summary>
    public List<object> ResponseStream { get; set; }

    [JsonIgnore]
    internal ServerCallContext ServerCallContext { get; set; }

    /// <summary>
    /// Gets the ServerCallContext related to this action
    /// </summary>
    public ServerCallContext GetServerCallContext()
    {
        return ServerCallContext;
    }

    [JsonExtensionData]
    public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
    
    public string ToJson()
    {
        return Configuration.JsonAdapter.Serialize(this);
    }
}
