using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;
using Audit.Core.Extensions;
using Audit.Core.Providers;

using Audit.MediatR.ConfigurationApi;

using MediatR;

using Moq;

using NUnit.Framework;

namespace Audit.MediatR.UnitTest;

[TestFixture]
public class AuditMediatRStreamBehaviorTests
{
    public class StreamPingRequest : IStreamRequest<StreamPong>
    {
        public string Message { get; set; }
    }

    public class StreamPong
    {
        public string Message { get; set; }
    }

    [SetUp]
    public void SetUp()
    {
        Configuration.Reset();
    }

    [Test]
    public void DefaultConstructor_Sets_Defaults()
    {
        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>();

        Assert.Multiple(() =>
        {
            Assert.That(behavior, Is.Not.Null);
            Assert.That(behavior.Options, Is.Not.Null);
            Assert.That(behavior.Options.DataProvider, Is.Null);
        });
    }

    [Test]
    public void OptionsConstructor_Sets_Options()
    {
        var options = new AuditMediatRConfigurator()
            .IncludeRequest(_ => true)
            .IncludeResponse(_ => true)
            .DataProvider(_ => new InMemoryDataProvider())
            .AuditScopeFactory(new AuditScopeFactory())
            .CallFilter(_ => true)
            .EventCreationPolicy(EventCreationPolicy.Manual)
            .Options;

        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>(options);

        Assert.That(behavior.Options, Is.SameAs(options));
    }

    [Test]
    public void ConfiguratorConstructor_Applies_Configuration()
    {
        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>(config =>
        {
            config.IncludeRequest();
            config.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
        });

        Assert.Multiple(() =>
        {
            Assert.That(behavior.Options, Is.Not.Null);
            Assert.That(behavior.Options.IncludeRequest, Is.Not.Null);
            Assert.That(behavior.Options.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnEnd));
        });
    }

    [Test]
    public async Task Handle_AuditDisabled_ShortCircuits_To_Next()
    {
        var request = new StreamPingRequest { Message = "hello" };
        var items = new[] { new StreamPong { Message = "world" } };

        var nextCalled = false;
        StreamHandlerDelegate<StreamPong> next = () =>
        {
            nextCalled = true;
            return items.ToAsyncEnumerable();
        };

        var options = new AuditMediatROptions
        {
            // CallFilter returning false disables auditing for this request
            CallFilter = _ => false
        };

        // AuditScopeFactory should not be invoked when auditing is disabled
        var auditScopeFactoryMock = new Mock<IAuditScopeFactory>(MockBehavior.Strict);
        options.AuditScopeFactory = auditScopeFactoryMock.Object;

        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>(options);

        var resultEnumerable = behavior.Handle(request, next, CancellationToken.None);
        var result = await resultEnumerable.ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(result, Is.EqualTo(items));
        });

        auditScopeFactoryMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_Includes_Request_And_Collects_ResponseStream_Items()
    {
        var request = new StreamPingRequest { Message = "stream" };
        var items = new[]
        {
            new StreamPong { Message = "a" },
            new StreamPong { Message = "b" },
            new StreamPong { Message = "c" }
        };

        var dp = new InMemoryDataProvider();

        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => true,
            // no single response payload in stream behavior, it records ResponseStream entries
            CallFilter = _ => true,
            DataProvider = _ => dp
        };

        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>(options);

        StreamHandlerDelegate<StreamPong> next = () => items.ToAsyncEnumerable();

        var resultEnumerable = behavior.Handle(request, next, CancellationToken.None);
        var result = await resultEnumerable.ToListAsync();

        var eventList = dp.GetAllEventsOfType<AuditEventMediatR>();

        Assert.That(eventList, Has.Count.EqualTo(1));

        var capturedEvent = eventList[0];
        var capturedCall = capturedEvent.Call;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(items));
            Assert.That(capturedEvent, Is.Not.Null);
            Assert.That(capturedCall, Is.Not.Null);
            Assert.That(capturedCall.CallType, Is.EqualTo(MediatRCallType.StreamRequest.ToString()));
            Assert.That(capturedCall.RequestType, Is.EqualTo(request.GetType().GetFullTypeName()));
            Assert.That(capturedCall.ResponseType, Is.EqualTo(typeof(StreamPong).GetFullTypeName()));
            Assert.That(capturedCall.Request, Is.SameAs(request));
            Assert.That(capturedCall.ResponseStream, Is.Not.Null);
            Assert.That(capturedCall.ResponseStream.Count, Is.EqualTo(items.Length));
            Assert.That(capturedCall.ResponseStream.Cast<StreamPong>().Select(x => x.Message),
                Is.EqualTo(items.Select(i => i.Message)));
            Assert.That(capturedCall.GetCallContext().Request, Is.SameAs(request));
            Assert.That(capturedCall.GetCallContext().RequestType, Is.EqualTo(typeof(StreamPingRequest)));
            Assert.That(capturedCall.GetCallContext().ResponseType, Is.EqualTo(typeof(StreamPong)));
            Assert.That(capturedCall.GetCallContext().CallType, Is.EqualTo(MediatRCallType.StreamRequest));
        });
    }

    [Test]
    public async Task Handle_Does_Not_Include_Request_When_Option_False()
    {
        var request = new StreamPingRequest { Message = "no-req" };
        var items = new[] { new StreamPong { Message = "x" }, new StreamPong { Message = "y" } };

        var dp = new InMemoryDataProvider();

        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => false,
            CallFilter = _ => true,
            DataProvider = _ => dp
        };

        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>(options);

        StreamHandlerDelegate<StreamPong> next = () => items.ToAsyncEnumerable();

        var resultEnumerable = behavior.Handle(request, next, CancellationToken.None);
        var result = await resultEnumerable.ToListAsync();

        var events = dp.GetAllEventsOfType<AuditEventMediatR>();

        Assert.That(events, Has.Count.EqualTo(1));

        var capturedCall = events[0].Call;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(items));
            Assert.That(capturedCall, Is.Not.Null);
            Assert.That(capturedCall.Request, Is.Null);
            Assert.That(capturedCall.ResponseStream.Count, Is.EqualTo(items.Length));
        });
    }

    [Test]
    public async Task Handle_Captures_Exception_And_Continues_Audit()
    {
        var request = new StreamPingRequest { Message = "boom" };

        var dp = new InMemoryDataProvider();

        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => true,
            CallFilter = _ => true,
            DataProvider = _ => dp
        };

        var behavior = new AuditMediatRStreamBehavior<StreamPingRequest, StreamPong>(options);

        // The stream throws on iteration
        StreamHandlerDelegate<StreamPong> next = () => ThrowingAsyncEnumerable(new InvalidOperationException("stream failed"));

        try
        {
            var resultEnumerable = behavior.Handle(request, next, CancellationToken.None);
            // consume stream to trigger exception
            await foreach (var _ in resultEnumerable)
            {
                // should not reach here
            }
            Assert.Fail("Expected exception was not thrown.");
        }
        catch (InvalidOperationException ex)
        {
            var events = dp.GetAllEventsOfType<AuditEventMediatR>();

            Assert.That(events, Has.Count.EqualTo(1));

            var capturedCall = events[0].Call;

            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Is.EqualTo("stream failed"));
                Assert.That(capturedCall, Is.Not.Null);
                Assert.That(capturedCall.Exception, Does.Contain(ex.Message));
                Assert.That(capturedCall.Request, Is.SameAs(request)); // request included before exception
                Assert.That(capturedCall.ResponseStream, Is.Empty);
            });
        }
    }

    private static async IAsyncEnumerable<StreamPong> ThrowingAsyncEnumerable(Exception ex)
    {
        await Task.Yield();
        throw ex;
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }
}

internal static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
}