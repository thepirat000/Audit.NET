using System;
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
public class AuditMediatRBehaviorTests
{
    public class PingRequest : IRequest<PongResponse>
    {
        public string Message { get; set; }
    }

    public class PongResponse
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
        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(behavior, Is.Not.Null);
            Assert.That(behavior.Options, Is.Null); 
        });
    }

    [Test]
    public void OptionsConstructor_Sets_Options()
    {
        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => true,
            IncludeResponse = _ => true
        };

        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(options);

        Assert.That(behavior.Options, Is.SameAs(options));
    }

    [Test]
    public void ConfiguratorConstructor_Applies_Configuration()
    {
        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(config =>
        {
            config.IncludeRequest();
            config.IncludeResponse();
            config.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
        });

        Assert.Multiple(() =>
        {
            Assert.That(behavior.Options, Is.Not.Null);
            Assert.That(behavior.Options.IncludeRequest, Is.Not.Null);
            Assert.That(behavior.Options.IncludeResponse, Is.Not.Null);
            Assert.That(behavior.Options.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnEnd));
        });
    }

    [Test]
    public async Task Handle_AuditDisabled_ShortCircuits_To_Next()
    {
        var request = new PingRequest { Message = "hello" };
        var response = new PongResponse { Message = "world" };

        var nextCalled = false;
        RequestHandlerDelegate<PongResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(response);
        };

        var options = new AuditMediatROptions
        {
            // CallFilter returning false disables auditing for this request
            CallFilter = _ => false
        };

        // AuditScopeFactory should not be invoked when auditing is disabled
        var auditScopeFactoryMock = new Mock<IAuditScopeFactory>(MockBehavior.Strict);
        options.AuditScopeFactory = auditScopeFactoryMock.Object;

        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(options);

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(result, Is.SameAs(response));
        });

        auditScopeFactoryMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_AuditDisabledGlobally_ShortCircuits_To_Next()
    {
        var request = new PingRequest { Message = "hello" };
        var response = new PongResponse { Message = "world" };
        
        Configuration.AuditDisabled = true;
        
        var nextCalled = false;
        RequestHandlerDelegate<PongResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(response);
        };

        var options = new AuditMediatROptions
        {
            CallFilter = _ => true
        };

        // AuditScopeFactory should not be invoked when auditing is disabled
        var auditScopeFactoryMock = new Mock<IAuditScopeFactory>(MockBehavior.Strict);
        options.AuditScopeFactory = auditScopeFactoryMock.Object;

        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(options);

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(result, Is.SameAs(response));
        });

        auditScopeFactoryMock.VerifyNoOtherCalls();
        Configuration.AuditDisabled = false;
    }

    [Test]
    public async Task Handle_Includes_Request_And_Response_Payloads()
    {
        var request = new PingRequest { Message = "req" };
        var response = new PongResponse { Message = "res" };

        var dp = new InMemoryDataProvider();

        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => true,
            IncludeResponse = _ => true,
            DataProvider = _ => dp
        };

        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(options);

        var next = new RequestHandlerDelegate<PongResponse>(_ => Task.FromResult(response));

        var result = await behavior.Handle(request, next, CancellationToken.None);

        var eventList = dp.GetAllEventsOfType<AuditEventMediatR>();

        Assert.That(eventList, Has.Count.EqualTo(1));

        var capturedEvent = eventList[0];
        var capturedCall = eventList[0].Call;

        var json = capturedCall.ToJson();

        var emptyCallContext = new MediatRCallContext();

        Assert.That(emptyCallContext.CallType, Is.EqualTo(MediatRCallType.Request));

        Assert.Multiple(() =>
        {

            Assert.That(result, Is.SameAs(response));
            Assert.That(json, Does.StartWith("{"));
            Assert.That(capturedEvent, Is.Not.Null);
            Assert.That(capturedCall, Is.Not.Null);
            Assert.That(capturedCall.CallType, Is.EqualTo(MediatRCallType.Request.ToString()));
            Assert.That(capturedCall.RequestType, Is.EqualTo(request.GetType().GetFullTypeName()));
            Assert.That(capturedCall.ResponseType, Is.EqualTo(typeof(PongResponse).GetFullTypeName()));
            Assert.That(capturedCall.Request, Is.SameAs(request));
            Assert.That(capturedCall.Response, Is.SameAs(response));
            Assert.That(capturedCall.GetCallContext().Request, Is.SameAs(request));
            Assert.That(capturedCall.GetCallContext().RequestType, Is.EqualTo(typeof(PingRequest)));
            Assert.That(capturedCall.GetCallContext().ResponseType, Is.EqualTo(typeof(PongResponse)));
            Assert.That(capturedCall.GetCallContext().CallType, Is.EqualTo(MediatRCallType.Request));
        });
    }

    [Test]
    public async Task Handle_Response_Not_Included_When_Option_False()
    {
        var request = new PingRequest { Message = "req" };
        var response = new PongResponse { Message = "res" };

        var dp = new InMemoryDataProvider();

        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => true,
            IncludeResponse = _ => false,
            CallFilter = _ => true,
            DataProvider = _ => dp
        };

        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(options);

        var next = new RequestHandlerDelegate<PongResponse>(_ => Task.FromResult(response));

        var result = await behavior.Handle(request, next, CancellationToken.None);

        var events = dp.GetAllEventsOfType<AuditEventMediatR>();

        Assert.That(events, Has.Count.EqualTo(1));

        var capturedCall = events[0].Call;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(response));
            Assert.That(capturedCall, Is.Not.Null);
            Assert.That(capturedCall.Response, Is.Null);
            Assert.That(capturedCall.Request, Is.SameAs(request));
        });
    }

    [Test]
    public void Handle_Captures_Exception_And_Rethrows()
    {
        var request = new PingRequest { Message = "boom" };

        var dp = new InMemoryDataProvider();

        var options = new AuditMediatROptions
        {
            IncludeRequest = _ => true,
            CallFilter = _ => true,
            DataProvider = _ => dp
        };

        var behavior = new AuditMediatRBehavior<PingRequest, PongResponse>(options);

        var expectedEx = new InvalidOperationException("handler failed");
        var next = new RequestHandlerDelegate<PongResponse>(_ =>
        {
            throw expectedEx;
        });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await behavior.Handle(request, next, CancellationToken.None);
        });

        var events = dp.GetAllEventsOfType<AuditEventMediatR>();
            
        Assert.That(events, Has.Count.EqualTo(1));
            
        var capturedCall = events[0].Call;

        Assert.Multiple(() =>
        {
            Assert.That(ex, Is.SameAs(expectedEx));
            Assert.That(capturedCall, Is.Not.Null);
            Assert.That(capturedCall.Exception, Does.Contain(expectedEx.Message));
            Assert.That(capturedCall.Request, Is.EqualTo(request)); // request included before exception
        });
    }

}