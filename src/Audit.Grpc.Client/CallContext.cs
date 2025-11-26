using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Audit.Grpc.Client;

/// <summary>
/// Call context information for gRPC client calls.
/// </summary>
public struct CallContext
{
    /// <summary>
    /// The gRPC method being called.
    /// </summary>
    public IMethod Method { get; set; }

    /// <summary>
    /// The call options for the gRPC call.
    /// </summary>
    public CallOptions Options { get; set; }

    /// <summary>
    /// Creates a CallContext from a ClientInterceptorContext.
    /// </summary>
    public static CallContext From<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        return new CallContext { Method = context.Method, Options = context.Options };
    }
}