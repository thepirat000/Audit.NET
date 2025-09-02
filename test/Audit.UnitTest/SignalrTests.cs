﻿#if NET462
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Audit.SignalR;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;

namespace Audit.UnitTest
{
    public class SignalrTests
    {
        [SetUp]
        public void Setup()
        {
            Configuration.Reset();
        }

        [Test]
        public void Test_Signalr_AuditDisabled()
        {
            var evs = new List<AuditEvent>();
            Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();

            var module = new TestAuditPipelineModule
            {
                AuditDisabled = true
            };

            SimulateConnectReconnectDisconnect(module);
            Assert.That(evs.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_Signalr_ConnectReconnectDisconnect()
        {
            var evs = new List<AuditEvent>();
            Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();


            var module = new TestAuditPipelineModule
            {
            };

            SimulateConnectReconnectDisconnect(module);

            Assert.That(evs.Count, Is.EqualTo(3));
            Assert.That(evs[0].EventType, Is.EqualTo("Connect"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventConnect>().EventType, Is.EqualTo(SignalrEventType.Connect));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventConnect>().ConnectionId, Is.EqualTo("x"));

            Assert.That(evs[1].EventType, Is.EqualTo("Reconnect"));
            Assert.That(evs[1].GetSignalrEvent<SignalrEventReconnect>().EventType, Is.EqualTo(SignalrEventType.Reconnect));
            Assert.That(evs[1].GetSignalrEvent<SignalrEventReconnect>().ConnectionId, Is.EqualTo("x"));

            Assert.That(evs[2].EventType, Is.EqualTo("Disconnect"));
            Assert.That(evs[2].GetSignalrEvent<SignalrEventDisconnect>().EventType, Is.EqualTo(SignalrEventType.Disconnect));
            Assert.That(evs[2].GetSignalrEvent<SignalrEventDisconnect>().ConnectionId, Is.EqualTo("x"));
            Assert.That(evs[2].GetSignalrEvent<SignalrEventDisconnect>().StopCalled, Is.EqualTo(true));
        }


        [Test]
        public void Test_Signalr_Incoming()
        {
            var evs = new List<AuditEvent>();
            Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();

            var module = new TestAuditPipelineModule();

            SimulateIncoming(module, "cnn-incoming", "send", new object[]{1, "two"});
            Task.Delay(50).Wait();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].EventType, Is.EqualTo("Incoming"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventIncoming>().EventType, Is.EqualTo(SignalrEventType.Incoming));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventIncoming>().ConnectionId, Is.EqualTo("cnn-incoming"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventIncoming>().MethodName, Is.EqualTo("send"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventIncoming>().Args[0], Is.EqualTo(1));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventIncoming>().Args[1], Is.EqualTo("two"));
        }

        [Test]
        public void Test_Signalr_Outgoing()
        {
            var evs = new List<AuditEvent>();
            Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();


            var module = new TestAuditPipelineModule();

            SimulateOutgoing(module, "cnn-Outgoing", "myhub", "signal", new object[] { "one", 2 });
            Task.Delay(50).Wait();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].EventType, Is.EqualTo("Outgoing"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventOutgoing>().EventType, Is.EqualTo(SignalrEventType.Outgoing));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventOutgoing>().Signal, Is.EqualTo("signal"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventOutgoing>().MethodName, Is.EqualTo("receive"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventOutgoing>().Args[0], Is.EqualTo("one"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventOutgoing>().Args[1], Is.EqualTo(2));
        }

        [Test]
        public void Test_Signalr_Error()
        {
            var evs = new List<AuditEvent>();
            Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();


            var module = new TestAuditPipelineModule();

            SimulateIncomingError(module, new ArgumentNullException("SomeParameter", "message"), "cnn-Error", "err", new object[] { 0 });
            Task.Delay(50).Wait();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].EventType, Is.EqualTo("Error"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventError>().EventType, Is.EqualTo(SignalrEventType.Error));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventError>().ConnectionId, Is.EqualTo("cnn-Error"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventError>().MethodName, Is.EqualTo("err"));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventError>().Args[0], Is.EqualTo(0));
            Assert.That(evs[0].GetSignalrEvent<SignalrEventError>().Exception.Contains("SomeParameter"), Is.True);
        }

        [Test]
        public void Test_Signalr_CustomAuditScopeFactory()
        {
            var evs = new List<AuditEvent>();
            Configuration.DataProvider = null;
            
            var dp = new DynamicDataProvider();
            dp.AttachOnInsertAndReplace(ev => { evs.Add(ev); });

            var factory = new Mock<IAuditScopeFactory>();
            factory.Setup(_ => _.Create(It.IsAny<AuditScopeOptions>()))
                .Returns(new AuditScope(new AuditScopeOptions() { DataProvider = dp, AuditEvent = new AuditEventSignalr() }));

            var module = new TestAuditPipelineModule()
            {
                AuditScopeFactory = factory.Object
            };

            SimulateIncomingError(module, new ArgumentNullException("SomeParameter", "message"), "cnn-Error", "err", new object[] { 0 });
            Task.Delay(50).Wait();

            Assert.That(evs.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetSignalrEvent_Returns_Event_From_AuditEventSignalr()
        {
            var expectedEvent = new TestSignalrEvent();
            var auditEvent = new AuditEventSignalr { Event = expectedEvent };

            var result = auditEvent.GetSignalrEvent();

            Assert.That(expectedEvent, Is.SameAs(result));
        }


        [Test]
        public void GetSignalrEvent_Returns_Null_For_NonSignalrEvent()
        {
            var auditEvent = new AuditEvent();

            var result = auditEvent.GetSignalrEvent();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetSignalrEvent_Generic_Returns_Event_As_T()
        {
            var expectedEvent = new TestSignalrEvent();
            var auditEvent = new AuditEventSignalr { Event = expectedEvent };

            var result = auditEvent.GetSignalrEvent<TestSignalrEvent>();

            Assert.That(expectedEvent, Is.SameAs(result));
        }

        [Test]
        public void GetSignalrEvent_Generic_Returns_Null_If_Not_T()
        {
            var auditEvent = new AuditEventSignalr { Event = new SignalrEventBase() };

            var result = auditEvent.GetSignalrEvent<TestSignalrEvent>();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetSignalrEvent_From_AuditScope_Returns_Event()
        {
            Configuration.Setup().UseNullProvider();

            var expectedEvent = new TestSignalrEvent();
            var auditEventSignalr = new AuditEventSignalr { Event = expectedEvent };
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                AuditEvent = auditEventSignalr
            });

            var result = scope.GetSignalrEvent();

            Assert.That(expectedEvent, Is.SameAs(result));
        }

        [Test]
        public void GetSignalrEvent_Generic_From_AuditScope_Returns_Event_As_T()
        {
            Configuration.Setup().UseNullProvider();

            var expectedEvent = new TestSignalrEvent();
            var auditEventSignalr = new AuditEventSignalr { Event = expectedEvent };
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                AuditEvent = auditEventSignalr
            });

            var result = scope.GetSignalrEvent<TestSignalrEvent>();

            Assert.That(expectedEvent, Is.SameAs(result));
        }

        [Test]
        public void GetIncomingAuditScope_Returns_Scope_If_Exists()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { AuditPipelineModule.AuditScopeIncomingEnvironmentKey, expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetIncomingAuditScope();

            Assert.That(expectedScope, Is.SameAs(result));
        }

        [Test]
        public void GetIncomingAuditScope_Returns_Null_If_Not_Found()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { "wrong-key", expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetIncomingAuditScope();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetConnectAuditScope_Returns_Scope_If_Exists()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { AuditPipelineModule.AuditScopeConnectEnvironmentKey, expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetConnectAuditScope();

            Assert.That(expectedScope, Is.SameAs(result));
        }

        [Test]
        public void GetConnectAuditScope_Returns_Null_If_Not_Found()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { "wrong-key", expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetConnectAuditScope();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetDisconnectAuditScope_Returns_Scope_If_Exists()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { AuditPipelineModule.AuditScopeDisconnectEnvironmentKey, expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetDisconnectAuditScope();

            Assert.That(expectedScope, Is.SameAs(result));
        }

        [Test]
        public void GetDisconnectAuditScope_Returns_Null_If_Not_Found()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { "wrong-key", expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetDisconnectAuditScope();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetReconnectAuditScope_Returns_Scope_If_Exists()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { AuditPipelineModule.AuditScopeReconnectEnvironmentKey, expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetReconnectAuditScope();

            Assert.That(expectedScope, Is.SameAs(result));
        }

        [Test]
        public void GetReconnectAuditScope_Returns_Null_If_Not_Found()
        {
            Configuration.Setup().UseNullProvider();
            var expectedScope = AuditScope.Create(new AuditScopeOptions()
            {

            });
            var env = new Dictionary<string, object>
            {
                { "wrong-key", expectedScope }
            };
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Environment).Returns(env);
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            var mockHub = new Mock<IHub>();
            mockHub.Setup(h => h.Context).Returns(mockContext.Object);

            var result = mockHub.Object.GetReconnectAuditScope();

            Assert.That(result, Is.Null);
        }
        
        [Test]
        public void AuditPipelineModule_Factory()
        {
            var result = AuditPipelineModule.Create(c => c.DisableAudit());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AuditDisabled, Is.True);
        }

        [Test]
        public void Test_Signalr_Stress()
        {
            var ids = new List<string>();
            var evs = new ConcurrentBag<AuditEvent>();
            Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();

            var threads = 25;
            var tasks = new Task<string>[threads];
            Trace.WriteLine($"Threads: {threads}");
            Console.WriteLine($"Threads: {threads}");
            var module = new TestAuditPipelineModule();

            for (int i = 0; i < threads; i++)
            {
                var cnnId = Guid.NewGuid().ToString();
                ids.Add(cnnId);
                tasks[i] = Task.Run(() => DoStress(module, cnnId));
            }

            Task.WaitAll(tasks);

            Task.Delay(1000).Wait();

            Assert.That(evs.Count, Is.EqualTo(6 * threads));
            for (int i = 0; i < threads; i++)
            {
                var id = ids[i];
                Assert.That(tasks[i].Result, Is.EqualTo(id));
                Assert.That(evs.Count(x => x.GetSignalrEvent<SignalrEventConnect>()?.ConnectionId == id), Is.EqualTo(1));
                Assert.That(evs.Count(x => x.GetSignalrEvent<SignalrEventDisconnect>()?.ConnectionId == id), Is.EqualTo(1));
                Assert.That(evs.Count(x => x.GetSignalrEvent<SignalrEventReconnect>()?.ConnectionId == id), Is.EqualTo(1));
                Assert.That(evs.Count(x => x.GetSignalrEvent<SignalrEventIncoming>()?.ConnectionId == id), Is.EqualTo(1));
                Assert.That(evs.Count(x => x.GetSignalrEvent<SignalrEventOutgoing>()?.Signal == "mysignal-" + id), Is.EqualTo(1));
                Assert.That(evs.Count(x => x.GetSignalrEvent<SignalrEventError>()?.ConnectionId == id), Is.EqualTo(1));
            }


        }

        private string DoStress(TestAuditPipelineModule module, string cnnId)
        {
            SimulateConnect(module, cnnId);
            Task.Delay(50).Wait();

            SimulateOutgoing(module, cnnId, "myhub", "mysignal-" + cnnId, null);
            Task.Delay(50).Wait();

            SimulateIncoming(module, cnnId, "method1", new object [] {cnnId});
            Task.Delay(50).Wait();

            SimulateReconnect(module, cnnId);
            Task.Delay(50).Wait();

            SimulateIncomingError(module, new ArgumentNullException("p", cnnId), cnnId, "method2", null );
            Task.Delay(50).Wait();

            SimulateDisconnect(module, cnnId);
            Task.Delay(50).Wait();

            return cnnId;
        }
        
        private void SimulateConnectReconnectDisconnect(TestAuditPipelineModule module)
        {
            SimulateConnect(module, "x");
            Task.Delay(50).Wait();
            SimulateReconnect(module, "x");
            Task.Delay(50).Wait();
            SimulateDisconnect(module, "x");
            Task.Delay(50).Wait();
        }

        private void SimulateConnect(TestAuditPipelineModule module, string cnnId)
        {
            var dict = new Dictionary<string, object>();
            var request = new Mock<IRequest>();
            request.Setup(x => x.Environment).Returns(() => dict);
            var hub = new Mock<IHub>();
            hub.Setup(x => x.Context.ConnectionId).Returns(cnnId);
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            module.OnBeforeConnect(hub.Object);
            Task.Delay(50).Wait();
            module.OnAfterConnect(hub.Object);
            Task.Delay(50).Wait();
        }

        private void SimulateReconnect(TestAuditPipelineModule module, string cnnId)
        {
            var dict = new Dictionary<string, object>();
            var request = new Mock<IRequest>();
            request.Setup(x => x.Environment).Returns(() => dict);
            var hub = new Mock<IHub>();
            hub.Setup(x => x.Context.ConnectionId).Returns(cnnId);
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            module.OnBeforeReconnect(hub.Object);
            Task.Delay(50).Wait();
            module.OnAfterReconnect(hub.Object);
            Task.Delay(50).Wait();
        }

        private void SimulateDisconnect(TestAuditPipelineModule module, string cnnId)
        {
            var dict = new Dictionary<string, object>();
            var request = new Mock<IRequest>();
            request.Setup(x => x.Environment).Returns(() => dict);
            var hub = new Mock<IHub>();
            hub.Setup(x => x.Context.ConnectionId).Returns(cnnId);
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            module.OnBeforeDisconnect(hub.Object, true);
            Task.Delay(50).Wait();
            module.OnAfterDisconnect(hub.Object, true);
            Task.Delay(50).Wait();
        }

        private void SimulateIncoming(TestAuditPipelineModule module, string cnnId, string method, object[] args)
        {
            var dict = new Dictionary<string, object>();
            var hub = new Mock<IHub>();
            hub.Setup(x => x.Context.ConnectionId).Returns(cnnId);
            var request = new Mock<IRequest>();
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            request.Setup(x => x.Environment).Returns(() => dict);
            var ctx = new Mock<IHubIncomingInvokerContext>();
            ctx.SetupGet(x => x.MethodDescriptor).Returns(() => new MethodDescriptor(){Name = method });
            ctx.SetupGet(x => x.Hub).Returns(() => hub.Object);
            ctx.SetupGet(x => x.Args).Returns(() => args.ToList());
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            module.OnBeforeIncoming(ctx.Object);
            Task.Delay(50).Wait();
            module.OnAfterIncoming("result", ctx.Object);
            Task.Delay(50).Wait();
        }

        private void SimulateOutgoing(TestAuditPipelineModule module, string cnnId, string hubName, string signal, object[] args)
        {
            var dict = new Dictionary<string, object>();
            var hub = new Mock<IHub>();
            hub.Setup(x => x.Context.ConnectionId).Returns(cnnId);
            var request = new Mock<IRequest>();
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            request.Setup(x => x.Environment).Returns(() => dict);
            var ctx = new Mock<IHubOutgoingInvokerContext>();
            ctx.SetupGet(x => x.Signal).Returns(() => signal);
            ctx.SetupGet(x => x.Invocation).Returns(() => new ClientHubInvocation(){Method = "receive", Args = args, Hub = hubName});
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            module.OnBeforeOutgoing(ctx.Object);
            Task.Delay(50).Wait();
            module.OnAfterOutgoing(ctx.Object);
            Task.Delay(50).Wait();
        }

        private void SimulateIncomingError(TestAuditPipelineModule module, Exception exception, string cnnId, string method, object[] args)
        {
            var dict = new Dictionary<string, object>();
            var hub = new Mock<IHub>();
            hub.Setup(x => x.Context.ConnectionId).Returns(cnnId);
            var request = new Mock<IRequest>();
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            request.Setup(x => x.Environment).Returns(() => dict);
            var ctx = new Mock<IHubIncomingInvokerContext>();
            ctx.SetupGet(x => x.MethodDescriptor).Returns(() => new MethodDescriptor() { Name = method });
            ctx.SetupGet(x => x.Hub).Returns(() => hub.Object);
            ctx.SetupGet(x => x.Args).Returns(() => args?.ToList());
            hub.SetupGet(x => x.Context.Request).Returns(() => request.Object);
            module.OnIncomingError(new ExceptionContext(exception), ctx.Object);
            Task.Delay(50).Wait();
        }
    }
    public class TestSignalrEvent : SignalrEventBase { }

    public class TestAuditPipelineModule : AuditPipelineModule
    {
        private object _locker = new object();
        public List<Tuple<string, object>> Events = new List<Tuple<string, object>>();

        public new bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            lock(_locker)
            {
                Events.Add(new Tuple<string, object>("OnBeforeIncoming", context));
            }

            return base.OnBeforeIncoming(context);
        }
        public new object OnAfterIncoming(object result, IHubIncomingInvokerContext context)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnAfterIncoming", context));
            }

            return base.OnAfterIncoming(result, context);
        }

        public new bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnBeforeOutgoing", context));
            }

            return base.OnBeforeOutgoing(context);
        }

        public new void OnAfterOutgoing(IHubOutgoingInvokerContext context)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnAfterOutgoing", context));
            }

            base.OnAfterOutgoing(context);
        }

        public new bool OnBeforeConnect(IHub hub)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnBeforeConnect", hub));
            }

            return base.OnBeforeConnect(hub);
        }

        public new void OnAfterConnect(IHub hub)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnAfterConnect", hub));
            }

            base.OnAfterConnect(hub);
        }

        public new bool OnBeforeDisconnect(IHub hub, bool stopCalled)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnBeforeDisconnect", hub));
            }

            return base.OnBeforeDisconnect(hub, stopCalled);
        }

        public new void OnAfterDisconnect(IHub hub, bool stopCalled)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnAfterDisconnect", hub));
            }

            base.OnAfterDisconnect(hub, stopCalled);
        }

        public new bool OnBeforeReconnect(IHub hub)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnBeforeReconnect", hub));
            }

            return base.OnBeforeReconnect(hub);
        }
        public new void OnAfterReconnect(IHub hub)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnAfterReconnect", hub));
            }

            base.OnAfterReconnect(hub);
        }

        public new void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            lock (_locker)
            {
                Events.Add(new Tuple<string, object>("OnIncomingError", exceptionContext));
            }

            base.OnIncomingError(exceptionContext, invokerContext);
        }
    }
}
#endif