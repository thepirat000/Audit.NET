using Audit.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Audit.DynamicProxy.UnitTest
{
    public class DynamicProxyTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Setup()
                .UseNullProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();
        }

        [Test]
        public async Task Test_CreationPolicyAync()
        {
            var inserts = new List<AuditEvent>();
            var replaces = new List<AuditEvent>(); 
            Audit.Core.Configuration.Setup()
                .Use(config => config.OnInsert(ev =>
                {
                    inserts.Add(AuditEvent.FromJson(ev.ToJson()));
                })
                .OnReplace((id, ev) =>
                {
                    replaces.Add(AuditEvent.FromJson(ev.ToJson()));
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var real = new InterceptMe("test");
            var audited = AuditProxy.Create<InterceptMeBase>(real);
            var res = await audited.AsyncFunctionAsync("2000");

            Assert.AreEqual("ok", res);
            Assert.AreEqual(1, inserts.Count);
            Assert.AreEqual(1, replaces.Count);

            Assert.IsNull(inserts[0].GetAuditInterceptEvent().Result);
            Assert.AreEqual("Task<String>", replaces[0].GetAuditInterceptEvent().Result.Type);
            Assert.AreEqual("ok", replaces[0].GetAuditInterceptEvent().Result.Value.ToString());
        }

        private static string ToJson(object obj)
        {
            return obj == null ? null : JsonSerializer.Serialize(obj);
        }
        
        [Test]
        public async Task Test_Async()
        {
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(config => config.OnInsert(ev =>
                {
                    logs.Add(ev);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var real = new InterceptMe("test");
            
            var audited = AuditProxy.Create<InterceptMeBase>(real);

            audited.AsyncReturningVoidAsync("1000");
            
            var res = await audited.AsyncFunctionAsync("2000");

            var t1 = audited.MethodThatReturnsATask("1000");
            Assert.AreEqual("InterceptMe.MethodThatReturnsATask", logs[2].EventType);
            t1.Start();
            var s = t1.Result;

            var task = audited.AsyncMethodAsync("600");
            task.Wait();

            Assert.Throws<AggregateException>(() =>
            {
                var t2 = audited.AsyncFunctionAsync("should throw");
                t2.Wait();
            });

            var source = new CancellationTokenSource();
            source.CancelAfter(TimeSpan.FromSeconds(1));
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await audited.AsyncMethodAsyncWithCancellation(source.Token);
            });
            
            Assert.AreEqual(6, logs.Count);
            
            Audit.Core.Configuration.AddOnSavingAction(scope =>
            {
                var interceptEvent = scope.GetAuditInterceptEvent();
                foreach (var arg in interceptEvent.Arguments)
                {
                    arg.Value = ToJson(arg.Value);
                }

                if (interceptEvent.Result != null)
                {
                    interceptEvent.Result.Value = ToJson(interceptEvent.Result.Value);
                }
            });


            // Aync returning void cannot be continued
            Assert.AreEqual("InterceptMe.AsyncReturningVoidAsync", logs[0].EventType);
            Assert.True(logs[0].Duration < 1000);
            Assert.True(logs[0].GetAuditInterceptEvent().IsAsync);
            Assert.Null(logs[0].GetAuditInterceptEvent().AsyncStatus);

            Assert.AreEqual("ok", res);
            Assert.AreEqual("InterceptMe.AsyncFunctionAsync", logs[1].EventType);
            Assert.AreEqual("ok", logs[1].GetAuditInterceptEvent().Result.Value);
            Assert.True(logs[1].Duration >= 2000);
            Assert.AreEqual(TaskStatus.RanToCompletion.ToString(), logs[1].GetAuditInterceptEvent().AsyncStatus);

            // Methods that returns a task (but are not async) are not continued
            Assert.AreEqual("InterceptMe.MethodThatReturnsATask", logs[2].EventType);
            Assert.True(logs[2].Duration < 1000);
            Assert.False(logs[2].GetAuditInterceptEvent().IsAsync);

            Assert.AreEqual("InterceptMe.AsyncMethodAsync", logs[3].EventType);
            Assert.True(logs[3].Duration >= 500);
            Assert.AreEqual(TaskStatus.RanToCompletion.ToString(), logs[3].GetAuditInterceptEvent().AsyncStatus);

            Assert.AreEqual("InterceptMe.AsyncFunctionAsync", logs[4].EventType);
            Assert.NotNull(logs[4].GetAuditInterceptEvent().Exception);
            Assert.AreEqual(TaskStatus.Faulted.ToString(), logs[4].GetAuditInterceptEvent().AsyncStatus);

            Assert.AreEqual("InterceptMe.AsyncMethodAsyncWithCancellation", logs[5].EventType);
            Assert.True(logs[5].Duration >= 1000);
            Assert.AreEqual(TaskStatus.Canceled.ToString(), logs[5].GetAuditInterceptEvent().AsyncStatus);
        }

        [Test]
        public void Test_Out_Ref_Ignore()
        {
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(config => config.OnInsert(ev =>
                {
                    logs.Add(ev);
                }));
            var real = new InterceptMe("test");
            var audited = AuditProxy.Create<IInterceptMe>(real);
            int i1 = 100;
            int i2 = 2;

            audited.RefParam("A", ref i1, () => 1);
            audited.OutParam("B", out i2, () => 2);

            Assert.AreEqual(2, logs[0].GetAuditInterceptEvent().Arguments.Count);
            Assert.AreEqual(1, logs[1].GetAuditInterceptEvent().Arguments.Count);
            Assert.Null(logs[0].GetAuditInterceptEvent().Result);

            Assert.AreEqual(100, logs[0].GetAuditInterceptEvent().Arguments[1].Value);
            Assert.AreEqual(101, logs[0].GetAuditInterceptEvent().Arguments[1].OutputValue);

            Assert.AreEqual(2, logs[1].GetAuditInterceptEvent().Arguments[0].Value);
            Assert.AreEqual(22, logs[1].GetAuditInterceptEvent().Arguments[0].OutputValue);
        }

        [Test]
        public void Test_Config_DynamicDataProvider()
        {
            var logs = new List<string>();
            Audit.Core.Configuration.Setup()
                .Use(config => config.OnInsert(ev =>
                {
                    logs.Add(ev.EventType);
                }));
            var real = new InterceptMe("test");
            var audited = AuditProxy.Create<IInterceptMe>(real);

            audited.GetHashCode();
            audited.SomeProperty = "543";
            var t = audited.SomeProperty;
            var str = audited.ReturnString("test");

            Assert.AreEqual(3, logs.Count);
            Assert.Contains("InterceptMe.get_SomeProperty", logs);
            Assert.Contains("InterceptMe.set_SomeProperty", logs);
            Assert.Contains("InterceptMe.ReturnString", logs);
        }

        [Test]
        public void Test_Config_DataProvider()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, null);
            var str = x.ReturnString(guid);
            Assert.AreEqual(guid.ToUpper(), str);
            Assert.AreEqual("test", x.GetSomePropValue());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.InsertEvent(It.Is<AuditEvent>(ev => (string)ev.GetAuditInterceptEvent().Arguments[0].Value == guid)), Times.Once);
        }

        [Test]
        public void Test_Config_Events()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = true });
            x.SomeEvent += (s, e) => { };
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = false });
            x.SomeEvent += (s, e) => { };
            Assert.AreEqual("test", x.GetSomePropValue());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_Config_Properties()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = true });
            x.SomeEvent += (s, e) => { };
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = false });
            x.SomeEvent += (s, e) => { };
            Assert.AreEqual("test", x.GetSomePropValue());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_Behavior_Exception()
        {
            var real = new InterceptMe("");
            var realMocked = new Mock<InterceptMe>();
            realMocked.Setup(_ => _.IamVirtual()).Throws(new ArgumentOutOfRangeException("test exception"));
            var provider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<InterceptMe>(realMocked.Object);
            var scope = AuditProxy.CurrentScope;
            Assert.Throws<ArgumentOutOfRangeException>(() => x.IamVirtual());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

    }
}
