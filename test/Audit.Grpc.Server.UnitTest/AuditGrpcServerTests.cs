#if NET6_0_OR_GREATER
using Audit.Core;

using Grpc.Core;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using TestGrpcService.Protos;

namespace Audit.Grpc.Server.UnitTest;

[TestFixture]
public class AuditServerInterceptorIntegrationTests
{
    private GrpcServerInterceptorWebAppFactory _factory;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new GrpcServerInterceptorWebAppFactory();
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
        _factory.DataProvider.ClearEvents();
        Configuration.AddOnSavingAction(scope =>
        {
            var action = scope.GetServerCallAction();

            var httpContext = action.GetServerCallContext().GetHttpContext();

            action.CustomFields["TraceId"] = httpContext.TraceIdentifier;
        });
    }

    [Test]
    public void UnaryCall_ShouldReturnEcho_OnSuccess()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var deadline = DateTime.UtcNow.AddYears(1);
        var request = new SimpleRequest { Message = "Hello, server interceptor!" };
        var expectedResponse = $"Echo: {request.Message}";
        var headers = new Metadata { { "request-header", "request-header-value" } };

        // Act
        var reply = client.UnaryCall(request, headers, deadline);

        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(reply.Reply, Is.EqualTo(expectedResponse));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var evt = auditEvents[0];
        var action = evt.Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.True);
            Assert.That(action.MethodType, Is.EqualTo("Unary"));
            Assert.That(action.MethodName, Is.EqualTo("/demo.DemoService/UnaryCall"));
            Assert.That(evt.EventType, Is.EqualTo("/demo.DemoService/UnaryCall"));
            Assert.That(action.Deadline, Is.Not.Null);
            Assert.That(action.Deadline.Value.ToString("yyyyMMddHHmmss"), Is.EqualTo(deadline.ToString("yyyyMMddHHmmss")));
            Assert.That(action.RequestHeaders, Is.Not.Empty);
            Assert.That(action.RequestHeaders.Exists(h => h.Key == "request-header"), Is.True);
            Assert.That(action.RequestHeaders.Find(h => h.Key == "request-header").Value, Is.EqualTo("request-header-value"));
            Assert.That((action.Request as SimpleRequest)?.Message, Is.EqualTo(request.Message));
            Assert.That(action.Response, Is.TypeOf<SimpleResponse>());
            Assert.That(((SimpleResponse)action.Response).Reply, Is.EqualTo(expectedResponse));
            Assert.That(action.StatusCode, Is.Null, "StatusCode remains null on success (server interceptor sets only on RpcException).");
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
            Assert.That(action.Trailers[0].Key, Is.EqualTo("trailer-key"));
            Assert.That(action.Trailers[0].Value, Is.EqualTo("trailer-value"));
            Assert.That(action.CustomFields["TraceId"].ToString(), Has.Length.GreaterThan(1));
        });
    }

    [Test]
    public void UnaryCall_ShouldReturnException_OnFailure()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var request = new SimpleRequest { Message = TestDemoService.FailMessage };

        // Act
        var ex = Assert.Throws<RpcException>(() => client.UnaryCall(request));
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(ex.Status.Detail, Is.EqualTo("Simulated failure"));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.EqualTo("Internal"));
            Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
            Assert.That(action.Exception, Does.Contain("Simulated failure"));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UnaryCall_ShouldReturnException_OnException()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var request = new SimpleRequest { Message = TestDemoService.FailMessageNonRpc };

        // Act
        var ex = Assert.Throws<RpcException>(() => client.UnaryCall(request));
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.Null);
            Assert.That(action.StatusDetail, Is.Null);
            Assert.That(action.Exception, Does.Contain("Simulated Exception"));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ClientStream_ShouldReturnSum_AndCaptureRequestStream_OnSuccess()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var deadline = DateTime.UtcNow.AddYears(1);
        var headers = new Metadata { { "request-header", "request-header-value" } };

        // Act
        var call = client.ClientStream(headers, deadline);
        await call.RequestStream.WriteAsync(new SumRequest { Value = 1 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 2 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 3 });
        await call.RequestStream.CompleteAsync();
        var reply = await call.ResponseAsync;

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(reply.Sum, Is.EqualTo(6));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.MethodType, Is.EqualTo("ClientStreaming"));
            Assert.That(action.IsSuccess, Is.True);
            Assert.That(action.RequestStream, Has.Count.EqualTo(3));
            Assert.That(((SumRequest)action.RequestStream[0]).Value, Is.EqualTo(1));
            Assert.That(((SumRequest)action.RequestStream[1]).Value, Is.EqualTo(2));
            Assert.That(((SumRequest)action.RequestStream[2]).Value, Is.EqualTo(3));
            Assert.That(action.Response, Is.TypeOf<SumResponse>());
            Assert.That(((SumResponse)action.Response).Sum, Is.EqualTo(6));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ClientStream_ShouldReturnException_OnFailure()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);

        // Act
        var call = client.ClientStream();
        await call.RequestStream.WriteAsync(new SumRequest { Value = 1 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 2, Name = TestDemoService.FailMessage });
        await call.RequestStream.CompleteAsync();
        var ex = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.EqualTo("Internal"));
            Assert.That(action.RequestStream, Has.Count.EqualTo(2));
            Assert.That(((SumRequest)action.RequestStream[1]).Name, Is.EqualTo(TestDemoService.FailMessage));
            Assert.That(action.Exception, Does.Contain("Simulated failure"));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ClientStream_ShouldReturnException_OnException()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);

        // Act
        var call = client.ClientStream();
        await call.RequestStream.WriteAsync(new SumRequest { Value = 1 });
        await call.RequestStream.WriteAsync(new SumRequest { Value = 2, Name = TestDemoService.FailMessageNonRpc });
        await call.RequestStream.CompleteAsync();
        var ex = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Unknown));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.Null);
            Assert.That(action.RequestStream, Has.Count.EqualTo(2));
            Assert.That(((SumRequest)action.RequestStream[1]).Name, Is.EqualTo(TestDemoService.FailMessageNonRpc));
            Assert.That(action.Exception, Does.Contain("Simulated Exception"));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ServerStream_ShouldReturnResponses_AndCaptureResponseStream_OnSuccess()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var deadline = DateTime.UtcNow.AddYears(1);
        var headers = new Metadata { { "request-header", "request-header-value" } };
        var request = new StreamRequest { Number = 3 };

        // Act
        var call = client.ServerStream(request, headers, deadline);
        var responses = new List<StreamResponse>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            responses.Add(call.ResponseStream.Current);
        }
        call.Dispose();

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(responses.Count, Is.EqualTo(3));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.MethodType, Is.EqualTo("ServerStreaming"));
            Assert.That(action.IsSuccess, Is.True);
            Assert.That((action.Request as StreamRequest)?.Number, Is.EqualTo(request.Number));
            Assert.That(action.ResponseStream, Has.Count.EqualTo(3));
            Assert.That(((StreamResponse)action.ResponseStream[0]).Result, Is.EqualTo(0));
            Assert.That(((StreamResponse)action.ResponseStream[1]).Result, Is.EqualTo(2));
            Assert.That(((StreamResponse)action.ResponseStream[2]).Result, Is.EqualTo(4));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ServerStream_ShouldReturnException_OnFailure()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var request = new StreamRequest { Number = 3, Name = TestDemoService.FailMessage };

        // Act
        var call = client.ServerStream(request);
        var ex = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseStream.MoveNext(CancellationToken.None));
        call.Dispose();

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.EqualTo("Internal"));
            Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
            Assert.That(action.Exception, Does.Contain("Simulated failure"));
            Assert.That(action.ResponseStream, Is.Null.Or.Empty);
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ServerStream_ShouldReturnException_OnException()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var request = new StreamRequest { Number = 3, Name = TestDemoService.FailMessageNonRpc };

        // Act
        var call = client.ServerStream(request);
        var ex = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseStream.MoveNext(CancellationToken.None));
        call.Dispose();

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Unknown));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.Null);
            Assert.That(action.StatusDetail, Is.Null);
            Assert.That(action.Exception, Does.Contain("Simulated Exception"));
            Assert.That(action.ResponseStream, Is.Null.Or.Empty);
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Chat_ShouldEchoMessages_AndCaptureBothStreams_OnSuccess()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var deadline = DateTime.UtcNow.AddYears(1);
        var headers = new Metadata { { "request-header", "request-header-value" } };
        var messages = new[] { "hello", "world", "grpc" };

        // Act
        var call = client.Chat(headers, deadline);
        foreach (var m in messages)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { From = "Client", Text = m });
        }
        await call.RequestStream.CompleteAsync();

        var received = new List<ChatMessage>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            received.Add(call.ResponseStream.Current);
        }
        call.Dispose();

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(received.Count, Is.EqualTo(messages.Length));
        for (int i = 0; i < messages.Length; i++)
        {
            Assert.That(received[i].Text, Is.EqualTo(messages[i]));
        }
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.MethodType, Is.EqualTo("DuplexStreaming"));
            Assert.That(action.IsSuccess, Is.True);
            Assert.That(action.RequestStream, Has.Count.EqualTo(messages.Length));
            Assert.That(action.ResponseStream, Has.Count.EqualTo(messages.Length));
            for (int i = 0; i < messages.Length; i++)
            {
                Assert.That(((ChatMessage)action.RequestStream[i]).Text, Is.EqualTo(messages[i]));
                Assert.That(((ChatMessage)action.ResponseStream[i]).Text, Is.EqualTo(messages[i]));
            }
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Chat_ShouldReturnException_OnFailure()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var messages = new[] { "hello", TestDemoService.FailMessage };

        // Act
        var call = client.Chat();
        foreach (var m in messages)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { From = "Client", Text = m });
        }
        await call.RequestStream.CompleteAsync();

        var received = new List<ChatMessage>();
        var ex = Assert.ThrowsAsync<RpcException>(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                received.Add(call.ResponseStream.Current);
            }
        });
        call.Dispose();

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.EqualTo("Internal"));
            Assert.That(action.StatusDetail, Is.EqualTo("Simulated failure"));
            Assert.That(action.Exception, Does.Contain("Simulated failure"));
            Assert.That(action.RequestStream, Has.Count.EqualTo(messages.Length));
            Assert.That(action.ResponseStream, Has.Count.EqualTo(1));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Chat_ShouldReturnException_OnException()
    {
        // Arrange
        var channel = _factory.CreateGrpcChannel();
        using var ch = channel;
        var client = new DemoService.DemoServiceClient(channel);
        var messages = new[] { "hello", TestDemoService.FailMessageNonRpc };

        // Act
        var call = client.Chat();
        foreach (var m in messages)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { From = "Client", Text = m });
        }
        await call.RequestStream.CompleteAsync();

        var received = new List<ChatMessage>();
        var ex = Assert.ThrowsAsync<RpcException>(async () =>
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                received.Add(call.ResponseStream.Current);
            }
        });
        call.Dispose();

        await Task.Delay(100);
        var auditEvents = _factory.DataProvider.GetAllEventsOfType<AuditEventGrpcServer>();

        // Assert
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.Unknown));
        Assert.That(auditEvents, Has.Count.EqualTo(1));
        var action = auditEvents[0].Action;
        Assert.Multiple(() =>
        {
            Assert.That(action.IsSuccess, Is.False);
            Assert.That(action.StatusCode, Is.Null);
            Assert.That(action.StatusDetail, Is.Null);
            Assert.That(action.Exception, Does.Contain("Simulated Exception"));
            Assert.That(action.RequestStream, Has.Count.EqualTo(messages.Length));
            Assert.That(action.ResponseStream, Has.Count.EqualTo(1));
            Assert.That(action.Trailers, Has.Count.EqualTo(1));
        });
    }
}

#endif