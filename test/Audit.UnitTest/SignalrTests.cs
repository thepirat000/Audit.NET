#if NET451
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Extensions;
using Audit.Core.Providers;
using Audit.SignalR;
using Castle.Components.DictionaryAdapter;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;

namespace Audit.UnitTest
{
    public class SignalrTests
    {
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

            Assert.AreEqual(0, evs.Count);
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

            Assert.AreEqual(3, evs.Count);
            Assert.AreEqual("Connect", evs[0].EventType);
            Assert.AreEqual(SignalrEventType.Connect, evs[0].GetSignalrEvent<SignalrEventConnect>().EventType);
            Assert.AreEqual("x", evs[0].GetSignalrEvent<SignalrEventConnect>().ConnectionId);

            Assert.AreEqual("Reconnect", evs[1].EventType);
            Assert.AreEqual(SignalrEventType.Reconnect, evs[1].GetSignalrEvent<SignalrEventReconnect>().EventType);
            Assert.AreEqual("x", evs[1].GetSignalrEvent<SignalrEventReconnect>().ConnectionId);

            Assert.AreEqual("Disconnect", evs[2].EventType);
            Assert.AreEqual(SignalrEventType.Disconnect, evs[2].GetSignalrEvent<SignalrEventDisconnect>().EventType);
            Assert.AreEqual("x", evs[2].GetSignalrEvent<SignalrEventDisconnect>().ConnectionId);
            Assert.AreEqual(true, evs[2].GetSignalrEvent<SignalrEventDisconnect>().StopCalled);
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

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("Incoming", evs[0].EventType);
            Assert.AreEqual(SignalrEventType.Incoming, evs[0].GetSignalrEvent<SignalrEventIncoming>().EventType);
            Assert.AreEqual("cnn-incoming", evs[0].GetSignalrEvent<SignalrEventIncoming>().ConnectionId);
            Assert.AreEqual("send", evs[0].GetSignalrEvent<SignalrEventIncoming>().MethodName);
            Assert.AreEqual(1, evs[0].GetSignalrEvent<SignalrEventIncoming>().Args[0]);
            Assert.AreEqual("two", evs[0].GetSignalrEvent<SignalrEventIncoming>().Args[1]);
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

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("Outgoing", evs[0].EventType);
            Assert.AreEqual(SignalrEventType.Outgoing, evs[0].GetSignalrEvent<SignalrEventOutgoing>().EventType);
            Assert.AreEqual("signal", evs[0].GetSignalrEvent<SignalrEventOutgoing>().Signal);
            Assert.AreEqual("receive", evs[0].GetSignalrEvent<SignalrEventOutgoing>().MethodName);
            Assert.AreEqual("one", evs[0].GetSignalrEvent<SignalrEventOutgoing>().Args[0]);
            Assert.AreEqual(2, evs[0].GetSignalrEvent<SignalrEventOutgoing>().Args[1]);
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

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("Error", evs[0].EventType);
            Assert.AreEqual(SignalrEventType.Error, evs[0].GetSignalrEvent<SignalrEventError>().EventType);
            Assert.AreEqual("cnn-Error", evs[0].GetSignalrEvent<SignalrEventError>().ConnectionId);
            Assert.AreEqual("err", evs[0].GetSignalrEvent<SignalrEventError>().MethodName);
            Assert.AreEqual(0, evs[0].GetSignalrEvent<SignalrEventError>().Args[0]);
            Assert.IsTrue(evs[0].GetSignalrEvent<SignalrEventError>().Exception.Contains("SomeParameter"));
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

            Assert.AreEqual(6 * threads, evs.Count);
            for (int i = 0; i < threads; i++)
            {
                var id = ids[i];
                Assert.AreEqual(id, tasks[i].Result);
                Assert.AreEqual(1, evs.Count(x => x.GetSignalrEvent<SignalrEventConnect>()?.ConnectionId == id));
                Assert.AreEqual(1, evs.Count(x => x.GetSignalrEvent<SignalrEventDisconnect>()?.ConnectionId == id));
                Assert.AreEqual(1, evs.Count(x => x.GetSignalrEvent<SignalrEventReconnect>()?.ConnectionId == id));
                Assert.AreEqual(1, evs.Count(x => x.GetSignalrEvent<SignalrEventIncoming>()?.ConnectionId == id));
                Assert.AreEqual(1, evs.Count(x => x.GetSignalrEvent<SignalrEventOutgoing>()?.Signal == "mysignal-" + id));
                Assert.AreEqual(1, evs.Count(x => x.GetSignalrEvent<SignalrEventError>()?.ConnectionId == id));
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