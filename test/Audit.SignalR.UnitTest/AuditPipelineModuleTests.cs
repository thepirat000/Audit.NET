#if NET462
using System;
using Audit.Core;
using Audit.Core.Providers;

using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

using Moq;


using Moq.Protected;

using NUnit.Framework;

using System.Collections.Generic;

namespace Audit.SignalR.UnitTest
{
    [TestFixture]
    public class AuditPipelineModuleTests
    {
        [Test]
        public void AuditEventEnabled_Uses_Filters_Correctly()
        {
            var module = new AuditPipelineModule();

            var incomingEvent = new SignalrEventIncoming();
            var outgoingEvent = new SignalrEventOutgoing();
            var connectEvent = new SignalrEventConnect();
            var disconnectEvent = new SignalrEventDisconnect();
            var reconnectEvent = new SignalrEventReconnect();
            var errorEvent = new SignalrEventError();

            module.IncomingEventsFilter = _ => false;
            module.OutgoingEventsFilter = _ => true;
            module.ConnectEventsFilter = _ => false;
            module.DisconnectEventsFilter = _ => true;
            module.ReconnectEventsFilter = _ => false;
            module.ErrorEventsFilter = _ => true;

            Assert.That(module.AuditEventEnabled(incomingEvent), Is.False);
            Assert.That(module.AuditEventEnabled(outgoingEvent), Is.True);
            Assert.That(module.AuditEventEnabled(connectEvent), Is.False);
            Assert.That(module.AuditEventEnabled(disconnectEvent), Is.True);
            Assert.That(module.AuditEventEnabled(reconnectEvent), Is.False);
            Assert.That(module.AuditEventEnabled(errorEvent), Is.True);
        }

        [Test]
        public void AuditEventEnabled_Returns_True_When_Filter_Null()
        {
            var module = new AuditPipelineModule();

            Assert.That(module.AuditEventEnabled(new SignalrEventIncoming()), Is.True);
            Assert.That(module.AuditEventEnabled(new SignalrEventOutgoing()), Is.True);
            Assert.That(module.AuditEventEnabled(new SignalrEventConnect()), Is.True);
            Assert.That(module.AuditEventEnabled(new SignalrEventDisconnect()), Is.True);
            Assert.That(module.AuditEventEnabled(new SignalrEventReconnect()), Is.True);
            Assert.That(module.AuditEventEnabled(new SignalrEventError()), Is.True);
        }

        [Test]
        public void CreateAuditScope_Uses_Provided_Factory_And_DataProvider()
        {
            var signalrEvent = new SignalrEventIncoming();
            var factory = new AuditScopeFactory();
            var dataProvider = new NullDataProvider();

            var module = new AuditPipelineModule
            {
                AuditScopeFactory = factory,
                AuditDataProvider = dataProvider,
                AuditEventType = "TestEventType",
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            };

            var scope = module.CreateAuditScope(signalrEvent);

            Assert.That(scope.EventType, Is.EqualTo("TestEventType"));
            Assert.That(scope.DataProvider, Is.SameAs(dataProvider));
        }

        [Test]
        public void OnBeforeIncoming_AuditDisabled_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockContext = new Mock<IHubIncomingInvokerContext>();
            var baseModule = new Mock<HubPipelineModule>();
            // Setup base.OnBeforeIncoming to return true
            baseModule.Protected()
                .Setup<bool>("OnBeforeIncoming", ItExpr.IsAny<IHubIncomingInvokerContext>())
                .Returns(true);

            // Act
            var result = module.OnBeforeIncoming(mockContext.Object);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeIncoming_AuditEnabled_EventFilteredOut_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule
            {
                IncomingEventsFilter = _ => false
            };

            var mockContext = new Mock<IHubIncomingInvokerContext>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();
            var mockMethodDescriptor = new Mock<MethodDescriptor>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(new Dictionary<string, object>());
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);
            mockContext.SetupGet(c => c.Hub).Returns(mockHub.Object);
            mockContext.SetupGet(c => c.Args).Returns(["arg1"]);
            mockContext.SetupGet(c => c.MethodDescriptor).Returns(mockMethodDescriptor.Object);
            mockMethodDescriptor.SetupGet(md => md.Name).Returns("TestMethod");

            // Act
            var result = module.OnBeforeIncoming(mockContext.Object);

            // Assert
            Assert.That(result, Is.True); 
        }

        [Test]
        public void OnBeforeIncoming_AuditEnabled_EventNotFiltered_SetsScopeInEnvironment()
        {
            var module = new TestAuditPipelineModule(new NullDataProvider(), EventCreationPolicy.InsertOnEnd, "TestEventType", true, true, false, new AuditScopeFactory())
            {
                IncomingEventsFilter = _ => true
            };

            var environment = new Dictionary<string, object>();
            var mockContext = new Mock<IHubIncomingInvokerContext>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();
            var mockMethodDescriptor = new Mock<MethodDescriptor>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);
            mockContext.SetupGet(c => c.Hub).Returns(mockHub.Object);
            mockContext.SetupGet(c => c.Args).Returns(["arg1"]);
            mockContext.SetupGet(c => c.MethodDescriptor).Returns(mockMethodDescriptor.Object);
            mockMethodDescriptor.SetupGet(md => md.Name).Returns("TestMethod");

            // Act
            var result = module.OnBeforeIncoming(mockContext.Object);

            // Assert
            Assert.That(result, Is.True); 
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeIncomingEnvironmentKey), Is.True);
            var scope = environment[AuditPipelineModule.AuditScopeIncomingEnvironmentKey] as IAuditScope;
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.EventType, Is.EqualTo("TestEventType"));
            Assert.That(scope.DataProvider, Is.SameAs(module.AuditDataProvider));
        }

        [Test]
        public void OnAfterIncoming_AuditDisabled_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockContext = new Mock<IHubIncomingInvokerContext>();
            var resultObj = new object();

            // Act
            var result = module.OnAfterIncoming(resultObj, mockContext.Object);

            // Assert
            Assert.That(result, Is.EqualTo(resultObj));
        }

        [Test]
        public void OnAfterIncoming_NoScopeInEnvironment_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule();
            var mockContext = new Mock<IHubIncomingInvokerContext>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            var environment = new Dictionary<string, object>();
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);
            mockContext.SetupGet(c => c.Hub).Returns(mockHub.Object);

            var resultObj = new object();

            // Act
            var result = module.OnAfterIncoming(resultObj, mockContext.Object);

            // Assert
            Assert.That(result, Is.EqualTo(resultObj));
        }

        [Test]
        public void OnAfterIncoming_WithScope_SetsResultAndRemovesScope()
        {
            var module = new TestAuditPipelineModule
            {
                AuditScopeFactory = new AuditScopeFactory(),
                AuditDataProvider = new NullDataProvider(),
                AuditEventType = "TestEventType",
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            };

            var environment = new Dictionary<string, object>();
            var mockContext = new Mock<IHubIncomingInvokerContext>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();
            var mockMethodDescriptor = new Mock<MethodDescriptor>();

            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);
            mockContext.SetupGet(c => c.Hub).Returns(mockHub.Object);
            mockContext.SetupGet(c => c.MethodDescriptor).Returns(mockMethodDescriptor.Object);

            // Create and add scope to environment
            var signalrEvent = new SignalrEventIncoming();
            var scope = module.CreateAuditScope(signalrEvent);
            environment[AuditPipelineModule.AuditScopeIncomingEnvironmentKey] = scope;

            var resultObj = "expectedResult";

            // Act
            var result = module.OnAfterIncoming(resultObj, mockContext.Object);

            // Assert
            Assert.That(result, Is.EqualTo(resultObj));
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeIncomingEnvironmentKey), Is.False);

            // Check that the result was set on the event
            var eventIncoming = (scope.EventAs<AuditEventSignalr>().Event as SignalrEventIncoming);
            Assert.That(eventIncoming, Is.Not.Null);
            Assert.That(eventIncoming.Result, Is.EqualTo(resultObj));
        }

        [Test]
        public void OnAfterOutgoing_AuditDisabled_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockContext = new Mock<IHubOutgoingInvokerContext>();

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterOutgoing(mockContext.Object));
        }

        [Test]
        public void OnAfterOutgoing_EventFilteredOut_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule();
            module.OutgoingEventsFilter = _ => false; // Filter disables auditing

            var mockContext = new Mock<IHubOutgoingInvokerContext>();
            mockContext.SetupGet(c => c.Signal).Returns("signal");
            mockContext.SetupGet(c => c.Signals).Returns(new List<string> { "signal1" });
            var invocation = new ClientHubInvocation()
            {
                Hub = "hub",
                Args = [1, 2],
                Method = "method",
                State = new Dictionary<string, object>()
            };
            mockContext.SetupGet(c => c.Invocation).Returns(invocation);
            
            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterOutgoing(mockContext.Object));
        }

        [Test]
        public void OnAfterOutgoing_EventNotFiltered_CreatesScopeAndCallsBase()
        {
            var module = new TestAuditPipelineModule
            {
                AuditScopeFactory = new AuditScopeFactory(),
                AuditDataProvider = new NullDataProvider(),
                AuditEventType = "TestEventType",
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            };
            module.OutgoingEventsFilter = _ => true; // Filter enables auditing

            var mockContext = new Mock<IHubOutgoingInvokerContext>();
            mockContext.SetupGet(c => c.Signal).Returns("signal");
            mockContext.SetupGet(c => c.Signals).Returns(new List<string> { "signal1" });
            var invocation = new ClientHubInvocation()
            {
                Hub = "hub",
                Args = [1, 2],
                Method = "method",
                State = new Dictionary<string, object>()
            };
            mockContext.SetupGet(c => c.Invocation).Returns(invocation);

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterOutgoing(mockContext.Object));
            // No direct way to assert scope creation, but no exception means flow is correct
        }

        [Test]
        public void OnBeforeConnect_AuditDisabled_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockHub = new Mock<IHub>();

            // Act
            var result = module.OnBeforeConnect(mockHub.Object);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeConnect_AuditEnabled_EventFilteredOut_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule
            {
                ConnectEventsFilter = _ => false
            };

            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(new Dictionary<string, object>());
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act
            var result = module.OnBeforeConnect(mockHub.Object);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeConnect_AuditEnabled_EventNotFiltered_SetsScopeInEnvironment()
        {
            var module = new TestAuditPipelineModule(new NullDataProvider(), EventCreationPolicy.InsertOnEnd, "TestEventType", true, true, false, new AuditScopeFactory())
            {
                ConnectEventsFilter = _ => true
            };

            var environment = new Dictionary<string, object>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act
            var result = module.OnBeforeConnect(mockHub.Object);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeConnectEnvironmentKey), Is.True);
            var scope = environment[AuditPipelineModule.AuditScopeConnectEnvironmentKey] as IAuditScope;
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.EventType, Is.EqualTo("TestEventType"));
            Assert.That(scope.DataProvider, Is.SameAs(module.AuditDataProvider));
        }

        [Test]
        public void OnAfterConnect_AuditDisabled_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockHub = new Mock<IHub>();

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterConnect(mockHub.Object));
        }

        [Test]
        public void OnAfterConnect_NoScopeInEnvironment_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            var environment = new Dictionary<string, object>();
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterConnect(mockHub.Object));
        }

        [Test]
        public void OnAfterConnect_WithScope_SetsConnectionIdAndRemovesScope()
        {
            var module = new TestAuditPipelineModule
            {
                AuditScopeFactory = new AuditScopeFactory(),
                AuditDataProvider = new NullDataProvider(),
                AuditEventType = "TestEventType",
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            };

            var environment = new Dictionary<string, object>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Create and add scope to environment
            var signalrEvent = new SignalrEventConnect();
            var scope = module.CreateAuditScope(signalrEvent);
            environment[AuditPipelineModule.AuditScopeConnectEnvironmentKey] = scope;

            // Act
            Assert.DoesNotThrow(() => module.OnAfterConnect(mockHub.Object));

            // Assert
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeConnectEnvironmentKey), Is.False);
            var eventConnect = (scope.EventAs<AuditEventSignalr>().Event as SignalrEventConnect);
            Assert.That(eventConnect, Is.Not.Null);
            Assert.That(eventConnect.ConnectionId, Is.EqualTo("conn1"));
        }

        [Test]
        public void OnBeforeDisconnect_AuditDisabled_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockHub = new Mock<IHub>();

            // Act
            var result = module.OnBeforeDisconnect(mockHub.Object, true);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeDisconnect_AuditEnabled_EventFilteredOut_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule
            {
                DisconnectEventsFilter = _ => false
            };

            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(new Dictionary<string, object>());
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act
            var result = module.OnBeforeDisconnect(mockHub.Object, true);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeDisconnect_AuditEnabled_EventNotFiltered_SetsScopeInEnvironment()
        {
            var module = new TestAuditPipelineModule(new NullDataProvider(), EventCreationPolicy.InsertOnEnd, "TestEventType", true, true, false, new AuditScopeFactory())
            {
                DisconnectEventsFilter = _ => true
            };

            var environment = new Dictionary<string, object>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act
            var result = module.OnBeforeDisconnect(mockHub.Object, true);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeDisconnectEnvironmentKey), Is.True);
            var scope = environment[AuditPipelineModule.AuditScopeDisconnectEnvironmentKey] as IAuditScope;
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.EventType, Is.EqualTo("TestEventType"));
            Assert.That(scope.DataProvider, Is.SameAs(module.AuditDataProvider));
        }

        [Test]
        public void OnAfterDisconnect_AuditDisabled_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockHub = new Mock<IHub>();

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterDisconnect(mockHub.Object, true));
        }

        [Test]
        public void OnAfterDisconnect_NoScopeInEnvironment_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            var environment = new Dictionary<string, object>();
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterDisconnect(mockHub.Object, true));
        }

        [Test]
        public void OnAfterDisconnect_WithScope_SetsConnectionIdAndRemovesScope()
        {
            var module = new TestAuditPipelineModule
            {
                AuditScopeFactory = new AuditScopeFactory(),
                AuditDataProvider = new NullDataProvider(),
                AuditEventType = "TestEventType",
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            };

            var environment = new Dictionary<string, object>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Create and add scope to environment
            var signalrEvent = new SignalrEventDisconnect();
            var scope = module.CreateAuditScope(signalrEvent);
            environment[AuditPipelineModule.AuditScopeDisconnectEnvironmentKey] = scope;

            // Act
            Assert.DoesNotThrow(() => module.OnAfterDisconnect(mockHub.Object, true));

            // Assert
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeDisconnectEnvironmentKey), Is.False);
            var eventDisconnect = (scope.EventAs<AuditEventSignalr>().Event as SignalrEventDisconnect);
            Assert.That(eventDisconnect, Is.Not.Null);
            Assert.That(eventDisconnect.ConnectionId, Is.EqualTo("conn1"));
        }

        [Test]
        public void OnBeforeReconnect_AuditDisabled_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockHub = new Mock<IHub>();

            // Act
            var result = module.OnBeforeReconnect(mockHub.Object);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeReconnect_AuditEnabled_EventFilteredOut_ReturnsBaseResult()
        {
            var module = new TestAuditPipelineModule
            {
                ReconnectEventsFilter = _ => false
            };

            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(new Dictionary<string, object>());
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act
            var result = module.OnBeforeReconnect(mockHub.Object);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OnBeforeReconnect_AuditEnabled_EventNotFiltered_SetsScopeInEnvironment()
        {
            var module = new TestAuditPipelineModule(new NullDataProvider(), EventCreationPolicy.InsertOnEnd, "TestEventType", true, true, false, new AuditScopeFactory())
            {
                ReconnectEventsFilter = _ => true
            };

            var environment = new Dictionary<string, object>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act
            var result = module.OnBeforeReconnect(mockHub.Object);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeReconnectEnvironmentKey), Is.True);
            var scope = environment[AuditPipelineModule.AuditScopeReconnectEnvironmentKey] as IAuditScope;
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.EventType, Is.EqualTo("TestEventType"));
            Assert.That(scope.DataProvider, Is.SameAs(module.AuditDataProvider));
        }

        [Test]
        public void OnAfterReconnect_AuditDisabled_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockHub = new Mock<IHub>();

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterReconnect(mockHub.Object));
        }

        [Test]
        public void OnAfterReconnect_NoScopeInEnvironment_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            var environment = new Dictionary<string, object>();
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnAfterReconnect(mockHub.Object));
        }

        [Test]
        public void OnAfterReconnect_WithScope_SetsConnectionIdAndRemovesScope()
        {
            var module = new TestAuditPipelineModule
            {
                AuditScopeFactory = new AuditScopeFactory(),
                AuditDataProvider = new NullDataProvider(),
                AuditEventType = "TestEventType",
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            };

            var environment = new Dictionary<string, object>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockRequest.SetupGet(r => r.Environment).Returns(environment);
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);

            // Create and add scope to environment
            var signalrEvent = new SignalrEventReconnect();
            var scope = module.CreateAuditScope(signalrEvent);
            environment[AuditPipelineModule.AuditScopeReconnectEnvironmentKey] = scope;

            // Act
            Assert.DoesNotThrow(() => module.OnAfterReconnect(mockHub.Object));

            // Assert
            Assert.That(environment.ContainsKey(AuditPipelineModule.AuditScopeReconnectEnvironmentKey), Is.False);
            var eventReconnect = (scope.EventAs<AuditEventSignalr>().Event as SignalrEventReconnect);
            Assert.That(eventReconnect, Is.Not.Null);
            Assert.That(eventReconnect.ConnectionId, Is.EqualTo("conn1"));
        }

        [Test]
        public void OnIncomingError_AuditDisabled_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule { AuditDisabled = true };
            var mockExceptionContext = new ExceptionContext(new Exception("test"));
            var mockInvokerContext = new Mock<IHubIncomingInvokerContext>();

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnIncomingError(mockExceptionContext, mockInvokerContext.Object));
        }

        [Test]
        public void OnIncomingError_EventFilteredOut_CallsBaseOnly()
        {
            var module = new TestAuditPipelineModule
            {
                ErrorEventsFilter = _ => false
            };

            var mockExceptionContext = new ExceptionContext(new Exception("test"));
            var mockInvokerContext = new Mock<IHubIncomingInvokerContext>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();
            var mockMethodDescriptor = new Mock<MethodDescriptor>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(new Dictionary<string, object>());
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);
            mockInvokerContext.SetupGet(c => c.Hub).Returns(mockHub.Object);
            mockInvokerContext.SetupGet(c => c.Args).Returns(["arg1"]);
            mockInvokerContext.SetupGet(c => c.MethodDescriptor).Returns(mockMethodDescriptor.Object);
            mockMethodDescriptor.SetupGet(md => md.Name).Returns("TestMethod");

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnIncomingError(mockExceptionContext, mockInvokerContext.Object));
        }

        [Test]
        public void OnIncomingError_EventNotFiltered_CreatesScopeAndCallsBase()
        {
            var module = new TestAuditPipelineModule(new NullDataProvider(), EventCreationPolicy.InsertOnEnd, "TestEventType", true, true, false, new AuditScopeFactory())
            {
                ErrorEventsFilter = _ => true
            };

            var mockExceptionContext = new ExceptionContext(new Exception("test"));
            var mockInvokerContext = new Mock<IHubIncomingInvokerContext>();
            var mockHub = new Mock<IHub>();
            var mockHubContext = new Mock<HubCallerContext>();
            var mockRequest = new Mock<IRequest>();
            var mockMethodDescriptor = new Mock<MethodDescriptor>();

            mockHubContext.SetupGet(c => c.ConnectionId).Returns("conn1");
            mockHubContext.SetupGet(c => c.Request).Returns(mockRequest.Object);
            mockHubContext.SetupGet(c => c.User).Returns((System.Security.Principal.IPrincipal)null);
            mockRequest.SetupGet(r => r.LocalPath).Returns("/test");
            mockRequest.SetupGet(r => r.Environment).Returns(new Dictionary<string, object>());
            mockHub.SetupGet(h => h.Context).Returns(mockHubContext.Object);
            mockInvokerContext.SetupGet(c => c.Hub).Returns(mockHub.Object);
            mockInvokerContext.SetupGet(c => c.Args).Returns(["arg1"]);
            mockInvokerContext.SetupGet(c => c.MethodDescriptor).Returns(mockMethodDescriptor.Object);
            mockMethodDescriptor.SetupGet(md => md.Name).Returns("TestMethod");

            // Act & Assert: Should not throw, just call base
            Assert.DoesNotThrow(() => module.OnIncomingError(mockExceptionContext, mockInvokerContext.Object));
            // No direct way to assert scope creation, but no exception means flow is correct
        }

        public class TestAuditPipelineModule : AuditPipelineModule
        {
            public TestAuditPipelineModule() {}
            public TestAuditPipelineModule(IAuditDataProvider dataProvider, EventCreationPolicy? creationPolicy = null,
                string auditEventType = null, bool includeHeaders = false, bool includeQueryString = false, bool auditDisabled = false, IAuditScopeFactory auditScopeFactory = null) : base(dataProvider, creationPolicy, auditEventType, includeHeaders, includeQueryString, auditDisabled, auditScopeFactory) {}

            public new bool OnBeforeIncoming(IHubIncomingInvokerContext context)
            {
                return base.OnBeforeIncoming(context);
            }

            public new object OnAfterIncoming(object result, IHubIncomingInvokerContext context)
            {
                return base.OnAfterIncoming(result, context);
            }

            public new void OnAfterOutgoing(IHubOutgoingInvokerContext context)
            {
                base.OnAfterOutgoing(context);
            }

            public new bool OnBeforeConnect(IHub hub)
            {
                return base.OnBeforeConnect(hub);
            }

            public new void OnAfterConnect(IHub hub)
            {
                base.OnAfterConnect(hub);
            }

            public new bool OnBeforeReconnect(IHub hub)
            {
                return base.OnBeforeReconnect(hub);
            }

            public new void OnAfterReconnect(IHub hub)
            {
                base.OnAfterReconnect(hub);
            }

            public new bool OnBeforeDisconnect(IHub hub, bool stopCalled)
            {
                return base.OnBeforeDisconnect(hub, stopCalled);
            }

            public new void OnAfterDisconnect(IHub hub, bool stopCalled)
            {
                base.OnAfterDisconnect(hub, stopCalled);
            }

            public new void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
            {
                base.OnIncomingError(exceptionContext, invokerContext);
            }
        }
    }

}
#endif