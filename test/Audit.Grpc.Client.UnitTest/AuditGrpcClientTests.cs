#if NET6_0_OR_GREATER
using Audit.Core;
using Audit.Core.Providers;

using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using NUnit.Framework;

using System;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core.Extensions;
using TestGrpcService.Protos;

namespace Audit.Grpc.Client.UnitTest;

[TestFixture]

public class AuditGrpcClientTests
{
    private GrpcWebAppFactoryFixture _factory;
    private const string FailMessage = "FAIL";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new GrpcWebAppFactoryFixture();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        Configuration.Reset();
        Configuration.DataProvider = new InMemoryDataProvider();
    }

    [Test]
    public void BlockingUnaryCall_ShouldReturnEcho_OnSuccess()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        var deadline = DateTime.UtcNow.AddYears(1);
        using var ch = channel;

        var testMessage = "Hello, interceptor!";
        var expectedResponse = $"Echo: {testMessage}";
        var request = new SimpleRequest() { Message = testMessage };

        var requestHeaders = new Metadata()
        {
            { "request-header", "request-header-value" }
        };

        // Act
        var reply = client.UnaryCall(request, requestHeaders, deadline);

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(reply.Reply, Is.EqualTo(expectedResponse));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.RequestHeaders, Has.Count.EqualTo(1));
        Assert.That(action.RequestHeaders[0].Key, Is.EqualTo("request-header"));
        Assert.That(action.RequestHeaders[0].Value, Is.EqualTo("request-header-value"));
        Assert.That(action.RequestHeaders[0].IsBinary, Is.False);
        Assert.That(action.Deadline, Is.EqualTo(deadline));
        Assert.That(action.Exception, Is.Null);
        Assert.That(action.IsSuccess, Is.True);
        Assert.That(action.FullName, Is.EqualTo("/demo.DemoService/UnaryCall"));
        Assert.That(action.MethodType, Is.EqualTo("Unary"));
        Assert.That(action.Request, Is.TypeOf<SimpleRequest>());
        Assert.That((action.Request as SimpleRequest)?.Message, Is.EqualTo(testMessage));
        Assert.That(action.MethodName, Is.EqualTo(nameof(TestDemoService.UnaryCall)));
        Assert.That(action.RequestType, Is.EqualTo(typeof(SimpleRequest).FullName));
        Assert.That(action.ResponseType, Is.EqualTo(typeof(SimpleResponse).FullName));
        Assert.That(action.Response, Is.TypeOf<SimpleResponse>());
        Assert.That((action.Response as SimpleResponse)?.Reply, Is.EqualTo(expectedResponse));
        Assert.That(action.StatusCode, Is.EqualTo("OK"));
        Assert.That(action.ServiceName, Is.EqualTo("demo.DemoService"));
        // Response headers and trailers are not captured in blocking calls
        Assert.That(action.ResponseHeaders, Is.Null);
        Assert.That(action.Trailers, Is.Null);
    }

    [Test]
    public async Task UnaryCallAsync_ShouldReturnEcho_OnSuccess()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        var deadline = DateTime.UtcNow.AddYears(1);
        using var ch = channel;

        var testMessage = "Hello, interceptor!";
        var expectedResponse = $"Echo: {testMessage}";
        var request = new SimpleRequest() { Message = testMessage };

        var requestHeaders = new Metadata()
        {
            { "request-header", "request-header-value" }
        };

        // Act
        var reply = await client.UnaryCallAsync(request, requestHeaders, deadline);

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(reply.Reply, Is.EqualTo(expectedResponse));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.RequestHeaders, Has.Count.EqualTo(1));
        Assert.That(action.RequestHeaders[0].Key, Is.EqualTo("request-header"));
        Assert.That(action.RequestHeaders[0].Value, Is.EqualTo("request-header-value"));
        Assert.That(action.RequestHeaders[0].IsBinary, Is.False);
        Assert.That(action.Deadline, Is.EqualTo(deadline));
        Assert.That(action.Exception, Is.Null);
        Assert.That(action.IsSuccess, Is.True);
        Assert.That(action.FullName, Is.EqualTo("/demo.DemoService/UnaryCall"));
        Assert.That(action.MethodType, Is.EqualTo("Unary"));
        Assert.That(action.Request, Is.TypeOf<SimpleRequest>());
        Assert.That((action.Request as SimpleRequest)?.Message, Is.EqualTo(testMessage));
        Assert.That(action.MethodName, Is.EqualTo(nameof(TestDemoService.UnaryCall)));
        Assert.That(action.RequestType, Is.EqualTo(typeof(SimpleRequest).FullName));
        Assert.That(action.ResponseType, Is.EqualTo(typeof(SimpleResponse).FullName));
        Assert.That(action.Response, Is.TypeOf<SimpleResponse>());
        Assert.That((action.Response as SimpleResponse)?.Reply, Is.EqualTo(expectedResponse));
        Assert.That(action.StatusCode, Is.EqualTo("OK"));
        Assert.That(action.StatusDetail, Is.EqualTo(""));
        Assert.That(action.ServiceName, Is.EqualTo("demo.DemoService"));
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));
        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public void UnaryCallAsync_ShouldReturnException_OnFailure()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        using var ch = channel;

        var request = new SimpleRequest() { Message = FailMessage };
        
        // Act
        var exception = Assert.ThrowsAsync<RpcException>(async () => await client.UnaryCallAsync(request));

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.Status.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(exception.Status.Detail, Is.EqualTo("Simulated failure"));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.StatusCode, Is.EqualTo("Internal"));
        Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
        Assert.That(action.IsSuccess, Is.False);
        Assert.That(exception.GetExceptionInfo(), Does.Contain(action.Exception));
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));
        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public async Task ClientStreamAsync_ShouldReturnSum_AndCaptureRequestStream_OnSuccess()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        var deadline = DateTime.UtcNow.AddYears(1);
        using var ch = channel;

        var requestHeaders = new Metadata()
        {
            { "request-header", "request-header-value" }
        };

        // Act
        var call = client.ClientStream(requestHeaders, deadline);
        // send 3 messages: 1, 2, 3 => sum = 6
        await call.RequestStream.WriteAsync(new SumRequest { Value = 1 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 2 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 3 });
        await call.RequestStream.CompleteAsync();
        var reply = await call.ResponseAsync;

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(reply.Sum, Is.EqualTo(6));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);

        // Request headers captured
        Assert.That(action.RequestHeaders, Has.Count.EqualTo(1));
        Assert.That(action.RequestHeaders[0].Key, Is.EqualTo("request-header"));
        Assert.That(action.RequestHeaders[0].Value, Is.EqualTo("request-header-value"));
        Assert.That(action.RequestHeaders[0].IsBinary, Is.False);

        // General call metadata
        Assert.That(action.Deadline, Is.EqualTo(deadline));
        Assert.That(action.Exception, Is.Null);
        Assert.That(action.IsSuccess, Is.True);
        Assert.That(action.FullName, Is.EqualTo("/demo.DemoService/ClientStream"));
        Assert.That(action.MethodType, Is.EqualTo("ClientStreaming"));
        Assert.That(action.MethodName, Is.EqualTo(nameof(TestDemoService.ClientStream)));
        Assert.That(action.RequestType, Is.EqualTo(typeof(SumRequest).FullName));
        Assert.That(action.ResponseType, Is.EqualTo(typeof(SumResponse).FullName));
        Assert.That(action.Response, Is.TypeOf<SumResponse>());
        Assert.That((action.Response as SumResponse)?.Sum, Is.EqualTo(6));
        Assert.That(action.StatusCode, Is.EqualTo("OK"));
        Assert.That(action.StatusDetail, Is.EqualTo(""));
        Assert.That(action.ServiceName, Is.EqualTo("demo.DemoService"));

        // Request stream captured
        Assert.That(action.RequestStream, Has.Count.EqualTo(3));
        Assert.That(action.RequestStream[0], Is.TypeOf<SumRequest>());
        Assert.That(((SumRequest)action.RequestStream[0]).Value, Is.EqualTo(1));
        Assert.That(((SumRequest)action.RequestStream[1]).Value, Is.EqualTo(2));
        Assert.That(((SumRequest)action.RequestStream[2]).Value, Is.EqualTo(3));

        // Response headers and trailers were added by the service
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));

        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public async Task ClientStreamAsync_ShouldReturnException_OnFailure()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        var deadline = DateTime.UtcNow.AddYears(1);
        using var ch = channel;

        var requestHeaders = new Metadata()
        {
            { "request-header", "request-header-value" }
        };

        // Act
        var call = client.ClientStream(requestHeaders, deadline);
        // send 3 messages: 1, 2, 3 => sum = 6
        await call.RequestStream.WriteAsync(new SumRequest { Value = 1 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 2, Name = FailMessage });
        await call.RequestStream.CompleteAsync();

        var exception = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
        
        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.Status.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(exception.Status.Detail, Is.EqualTo("Simulated failure"));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.StatusCode, Is.EqualTo("Internal"));
        Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
        Assert.That(action.IsSuccess, Is.False);
        Assert.That(exception.GetExceptionInfo(), Does.Contain(action.Exception));
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));
        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public async Task ServerStreamAsync_ShouldReturnResponses_AndCaptureResponseStream_OnSuccess()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        var deadline = DateTime.UtcNow.AddYears(1);
        using var ch = channel;

        var request = new StreamRequest() { Number = 3 };

        var requestHeaders = new Metadata()
        {
            { "request-header", "request-header-value" }
        };

        // Act
        var call = client.ServerStream(request, requestHeaders, deadline);

        var responses = new System.Collections.Generic.List<StreamResponse>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            responses.Add(call.ResponseStream.Current);
        }

        call.Dispose();

        await Task.Delay(500);

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(responses.Count, Is.EqualTo(3));
        Assert.That(responses[0].Result, Is.EqualTo(0));
        Assert.That(responses[1].Result, Is.EqualTo(2));
        Assert.That(responses[2].Result, Is.EqualTo(4));

        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);

        // Request captured and metadata
        Assert.That(action.Request, Is.TypeOf<StreamRequest>());
        Assert.That((action.Request as StreamRequest)?.Number, Is.EqualTo(3));
        Assert.That(action.RequestHeaders, Has.Count.EqualTo(1));
        Assert.That(action.RequestHeaders[0].Key, Is.EqualTo("request-header"));
        Assert.That(action.RequestHeaders[0].Value, Is.EqualTo("request-header-value"));
        Assert.That(action.Deadline, Is.EqualTo(deadline));
        Assert.That(action.Exception, Is.Null);
        Assert.That(action.IsSuccess, Is.True);
        Assert.That(action.FullName, Is.EqualTo("/demo.DemoService/ServerStream"));
        Assert.That(action.MethodType, Is.EqualTo("ServerStreaming"));
        Assert.That(action.MethodName, Is.EqualTo(nameof(TestDemoService.ServerStream)));
        Assert.That(action.RequestType, Is.EqualTo(typeof(StreamRequest).FullName));
        Assert.That(action.ResponseType, Is.EqualTo(typeof(StreamResponse).FullName));
        Assert.That(action.StatusCode, Is.EqualTo("OK"));
        Assert.That(action.StatusDetail, Is.EqualTo(""));
        Assert.That(action.ServiceName, Is.EqualTo("demo.DemoService"));

        // Response stream captured
        Assert.That(action.ResponseStream, Has.Count.EqualTo(3));
        Assert.That((action.ResponseStream[0] as StreamResponse)?.Result, Is.EqualTo(0));
        Assert.That((action.ResponseStream[1] as StreamResponse)?.Result, Is.EqualTo(2));
        Assert.That((action.ResponseStream[2] as StreamResponse)?.Result, Is.EqualTo(4));

        // Response headers and trailers added by service
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));

        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public async Task ServerStreamAsync_ShouldReturnException_OnFailure()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        using var ch = channel;

        var request = new StreamRequest() { Number = 3, Name = FailMessage };

        // Act
        var call = client.ServerStream(request);

        var exception = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseStream.MoveNext(CancellationToken.None));

        call.Dispose();

        await Task.Delay(500);

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.Status.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(exception.Status.Detail, Is.EqualTo("Simulated failure"));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.StatusCode, Is.EqualTo("Internal"));
        Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
        Assert.That(action.IsSuccess, Is.False);
        Assert.That(exception.GetExceptionInfo(), Does.Contain(action.Exception));
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));
        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public async Task ChatAsync_ShouldEchoMessages_AndCaptureBothStreams_OnSuccess()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        var deadline = DateTime.UtcNow.AddYears(1);
        using var ch = channel;

        var requestHeaders = new Metadata()
        {
            { "request-header", "request-header-value" }
        };

        var messagesToSend = new[] { "hello", "world", "grpc" };

        // Act
        var call = client.Chat(requestHeaders, deadline);

        // Send messages
        foreach (var text in messagesToSend)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { From = "Client", Text = text });
        }
        await call.RequestStream.CompleteAsync();

        // Read responses
        var responses = new System.Collections.Generic.List<ChatMessage>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            responses.Add(call.ResponseStream.Current);
        }

        call.Dispose();

        await Task.Delay(500);

        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert - responses
        Assert.That(responses.Count, Is.EqualTo(messagesToSend.Length));
        for (int i = 0; i < messagesToSend.Length; i++)
        {
            Assert.That(responses[i].From, Is.EqualTo("Server"));
            Assert.That(responses[i].Text, Is.EqualTo(messagesToSend[i]));
        }

        // Assert - audit event
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);

        // Headers and metadata
        Assert.That(action.RequestHeaders, Has.Count.EqualTo(1));
        Assert.That(action.RequestHeaders[0].Key, Is.EqualTo("request-header"));
        Assert.That(action.RequestHeaders[0].Value, Is.EqualTo("request-header-value"));
        Assert.That(action.RequestHeaders[0].IsBinary, Is.False);
        Assert.That(action.Deadline, Is.EqualTo(deadline));
        Assert.That(action.Exception, Is.Null);
        Assert.That(action.IsSuccess, Is.True);
        Assert.That(action.FullName, Is.EqualTo("/demo.DemoService/Chat"));
        Assert.That(action.MethodType, Is.EqualTo("DuplexStreaming"));
        Assert.That(action.MethodName, Is.EqualTo(nameof(TestDemoService.Chat)));
        Assert.That(action.RequestType, Is.EqualTo(typeof(ChatMessage).FullName));
        Assert.That(action.ResponseType, Is.EqualTo(typeof(ChatMessage).FullName));
        Assert.That(action.ServiceName, Is.EqualTo("demo.DemoService"));

        // Request stream captured
        Assert.That(action.RequestStream, Has.Count.EqualTo(messagesToSend.Length));
        for (int i = 0; i < messagesToSend.Length; i++)
        {
            Assert.That((action.RequestStream[i] as ChatMessage)?.Text, Is.EqualTo(messagesToSend[i]));
            Assert.That((action.RequestStream[i] as ChatMessage)?.From, Is.EqualTo("Client"));
        }

        // Response stream captured
        Assert.That(action.ResponseStream, Has.Count.EqualTo(messagesToSend.Length));
        for (int i = 0; i < messagesToSend.Length; i++)
        {
            Assert.That((action.ResponseStream[i] as ChatMessage)?.From, Is.EqualTo("Server"));
            Assert.That((action.ResponseStream[i] as ChatMessage)?.Text, Is.EqualTo(messagesToSend[i]));
        }

        // Response headers and trailers added by the service
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));

        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    [Test]
    public async Task ChatAsync_ShouldReturnException_OnFailure()
    {
        // Arrange
        var (client, channel, dp) = CreateClient();

        using var ch = channel;

        var messagesToSend = new[] { "hello", FailMessage };

        // Act
        var call = client.Chat();

        // Send messages
        foreach (var text in messagesToSend)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { From = "Client", Text = text });
        }
        await call.RequestStream.CompleteAsync();

        // Read responses
        var responses = new System.Collections.Generic.List<ChatMessage>();
        var exception = Assert.ThrowsAsync<RpcException>(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                responses.Add(call.ResponseStream.Current);
            }
        });

        call.Dispose();

        await Task.Delay(500);
        
        var auditEvents = dp.GetAllEventsOfType<AuditEventGrpcClient>();

        // Assert
        Assert.That(responses, Has.Count.EqualTo(1));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.Status.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(exception.Status.Detail, Is.EqualTo("Simulated failure"));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.StatusCode, Is.EqualTo("Internal"));
        Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
        Assert.That(action.IsSuccess, Is.False);
        Assert.That(exception.GetExceptionInfo(), Does.Contain(action.Exception));
        Assert.That(action.ResponseHeaders, Has.Count.EqualTo(1));
        Assert.That(action.ResponseHeaders[0].Key, Is.EqualTo("response-custom-header"));
        Assert.That(action.ResponseHeaders[0].Value, Is.EqualTo("value"));
        Assert.That(action.Trailers, Has.Count.EqualTo(1));
        Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
        Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
    }

    private (DemoService.DemoServiceClient Client, GrpcChannel channel, InMemoryDataProvider DataProvider) 
        CreateClient(Action<ConfigurationApi.IAuditClientInterceptorConfigurator> config = null, EventCreationPolicy creationPolicy = EventCreationPolicy.InsertOnEnd)
    {
        config ??= cfg => cfg.CallFilter(_ => true).IncludeRequestHeaders().IncludeRequestPayload().IncludeResponseHeaders().IncludeResponsePayload().IncludeTrailers();

        var dp = new InMemoryDataProvider();
        var interceptor = new AuditClientInterceptor(config);
        interceptor.DataProvider = dp;
        interceptor.EventCreationPolicy = creationPolicy;

        var channel = _factory.CreateGrpcChannel();

        var invoker = channel.Intercept(interceptor);
        
        var client = new DemoService.DemoServiceClient(invoker);
        
        return (client, channel, dp);
    }
}

#endif
