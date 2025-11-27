#if NET6_0_OR_GREATER
using System;
using Grpc.Core;

using System.Threading.Tasks;

using TestGrpcService.Protos;

namespace Audit.Grpc.Server.UnitTest
{
    public class TestDemoService : DemoService.DemoServiceBase
    {
        public const string FailMessage = "FAIL";
        public const string FailMessageNonRpc = "FAIL-NONRPC";

        public override async Task<SimpleResponse> UnaryCall(SimpleRequest request, ServerCallContext context)
        {
            await AddHeadersAndTrailers(context);

            if (request.Message == FailMessage)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Simulated failure"));
            }
            if (request.Message == FailMessageNonRpc)
            {
                throw new Exception("Simulated Exception");
            }

            return new SimpleResponse { Reply = "Echo: " + request.Message };
        }

        public override async Task ServerStream(StreamRequest request, IServerStreamWriter<StreamResponse> responseStream, ServerCallContext context)
        {
            await AddHeadersAndTrailers(context);

            if (request.Name == FailMessage)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Simulated failure"));
            }
            if (request.Name == FailMessageNonRpc)
            {
                throw new Exception("Simulated Exception");
            }

            for (int i = 0; i < request.Number; i++)
            {
                await responseStream.WriteAsync(new StreamResponse { Result = i * 2 });
            }
        }

        public override async Task<SumResponse> ClientStream(IAsyncStreamReader<SumRequest> requestStream, ServerCallContext context)
        {
            await AddHeadersAndTrailers(context);

            int sum = 0;
            await foreach (var msg in requestStream.ReadAllAsync())
            {
                if (msg.Name == FailMessage)
                {
                    throw new RpcException(new Status(StatusCode.Internal, "Simulated failure"));
                }
                if (msg.Name == FailMessageNonRpc)
                {
                    throw new Exception("Simulated Exception");
                }

                sum += msg.Value;
            }

            return new SumResponse { Sum = sum };
        }

        public override async Task Chat(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
        {
            await AddHeadersAndTrailers(context);

            await foreach (var msg in requestStream.ReadAllAsync())
            {
                if (msg.Text == FailMessage)
                {
                    throw new RpcException(new Status(StatusCode.Internal, "Simulated failure"));
                }
                if (msg.Text == FailMessageNonRpc)
                {
                    throw new Exception("Simulated Exception");
                }
                // echo back
                await responseStream.WriteAsync(new ChatMessage { From = "Server", Text = msg.Text });
            }
        }

        private static async Task AddHeadersAndTrailers(ServerCallContext context)
        {
            var headers = new Metadata { { "response-custom-header", "value" } };
            await context.WriteResponseHeadersAsync(headers);
            context.ResponseTrailers.Add("trailer-key", "trailer-value");
        }
    }
}
#endif