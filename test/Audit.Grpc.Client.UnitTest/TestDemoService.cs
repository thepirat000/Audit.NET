#if NET6_0_OR_GREATER
using Grpc.Core;

using System.Threading.Tasks;

using TestGrpcService.Protos;

namespace Audit.Grpc.Client.UnitTest
{
    public class TestDemoService : DemoService.DemoServiceBase
    {
        private const string FailMessage = "FAIL";

        public override async Task<SimpleResponse> UnaryCall(SimpleRequest request, ServerCallContext context)
        {
            await AddHeadersAndTrailers(context);

            if (request.Message == FailMessage)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Simulated failure"));
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