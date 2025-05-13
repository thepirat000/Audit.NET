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

            Assert.That(res, Is.EqualTo("ok"));
            Assert.That(inserts.Count, Is.EqualTo(1));
            Assert.That(replaces.Count, Is.EqualTo(1));

            Assert.That(inserts[0].GetAuditInterceptEvent().Result, Is.Null);
            Assert.That(replaces[0].GetAuditInterceptEvent().Result.Type, Is.EqualTo("Task<String>"));
            Assert.That(replaces[0].GetAuditInterceptEvent().Result.Value.ToString(), Is.EqualTo("ok"));
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
            Assert.That(logs[2].EventType, Is.EqualTo("InterceptMe.MethodThatReturnsATask"));
            t1.Start();
            var s = t1.Result;

            var task = audited.AsyncMethodAsync("600");
            task.Wait();

            Assert.Throws<AggregateException>(() =>
            {
                var t2 = audited.AsyncFunctionAsync("should throw");
                t2.Wait();
            });

            using var source = new CancellationTokenSource();
            source.CancelAfter(TimeSpan.FromSeconds(1));
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await audited.AsyncMethodAsyncWithCancellation(source.Token);
            });

            Assert.That(logs.Count, Is.EqualTo(6));
            
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
            Assert.That(logs[0].EventType, Is.EqualTo("InterceptMe.AsyncReturningVoidAsync"));
            Assert.True(logs[0].Duration < 1000);
            Assert.True(logs[0].GetAuditInterceptEvent().IsAsync);
            Assert.Null(logs[0].GetAuditInterceptEvent().AsyncStatus);

            Assert.That(res, Is.EqualTo("ok"));
            Assert.That(logs[1].EventType, Is.EqualTo("InterceptMe.AsyncFunctionAsync"));
            Assert.That(logs[1].GetAuditInterceptEvent().Result.Value, Is.EqualTo("ok"));
            Assert.True(logs[1].Duration >= 1000);
            Assert.That(logs[1].GetAuditInterceptEvent().AsyncStatus, Is.EqualTo(TaskStatus.RanToCompletion.ToString()));

            // Methods that returns a task (but are not async) are not continued
            Assert.That(logs[2].EventType, Is.EqualTo("InterceptMe.MethodThatReturnsATask"));
            Assert.True(logs[2].Duration < 1000);
            Assert.False(logs[2].GetAuditInterceptEvent().IsAsync);

            Assert.That(logs[3].EventType, Is.EqualTo("InterceptMe.AsyncMethodAsync"));
            Assert.True(logs[3].Duration >= 500);
            Assert.That(logs[3].GetAuditInterceptEvent().AsyncStatus, Is.EqualTo(TaskStatus.RanToCompletion.ToString()));

            Assert.That(logs[4].EventType, Is.EqualTo("InterceptMe.AsyncFunctionAsync"));
            Assert.NotNull(logs[4].GetAuditInterceptEvent().Exception);
            Assert.That(logs[4].GetAuditInterceptEvent().AsyncStatus, Is.EqualTo(TaskStatus.Faulted.ToString()));

            Assert.That(logs[5].EventType, Is.EqualTo("InterceptMe.AsyncMethodAsyncWithCancellation"));
            Assert.True(logs[5].Duration >= 1000);
            Assert.That(logs[5].GetAuditInterceptEvent().AsyncStatus, Is.EqualTo(TaskStatus.Canceled.ToString()));
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

            Assert.That(logs[0].GetAuditInterceptEvent().Arguments.Count, Is.EqualTo(2));
            Assert.That(logs[1].GetAuditInterceptEvent().Arguments.Count, Is.EqualTo(1));
            Assert.Null(logs[0].GetAuditInterceptEvent().Result);

            Assert.That(logs[0].GetAuditInterceptEvent().Arguments[1].Value, Is.EqualTo(100));
            Assert.That(logs[0].GetAuditInterceptEvent().Arguments[1].OutputValue, Is.EqualTo(101));

            Assert.That(logs[1].GetAuditInterceptEvent().Arguments[0].Value, Is.EqualTo(2));
            Assert.That(logs[1].GetAuditInterceptEvent().Arguments[0].OutputValue, Is.EqualTo(22));
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

            Assert.That(logs.Count, Is.EqualTo(3));
            Assert.Contains("InterceptMe.get_SomeProperty", logs);
            Assert.Contains("InterceptMe.set_SomeProperty", logs);
            Assert.Contains("InterceptMe.ReturnString", logs);
        }

        [Test]
        public void Test_Config_DataProvider()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<IAuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, null);
            var str = x.ReturnString(guid);
            Assert.That(str, Is.EqualTo(guid.ToUpper()));
            Assert.That(x.GetSomePropValue(), Is.EqualTo("test"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.InsertEvent(It.Is<AuditEvent>(ev => (string)ev.GetAuditInterceptEvent().Arguments[0].Value == guid)), Times.Once);
        }

        [Test]
        public void Test_Config_Events()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<IAuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = true });
            x.SomeEvent += (s, e) => { };
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = false });
            x.SomeEvent += (s, e) => { };
            Assert.That(x.GetSomePropValue(), Is.EqualTo("test"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_Config_Properties()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<IAuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = true });
            x.SomeEvent += (s, e) => { };
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            x = AuditProxy.Create<IInterceptMe>(real, new InterceptionSettings() { IgnoreEvents = false });
            x.SomeEvent += (s, e) => { };
            Assert.That(x.GetSomePropValue(), Is.EqualTo("test"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_Behavior_Exception()
        {
            var real = new InterceptMe("");
            var realMocked = new Mock<InterceptMe>();
            realMocked.Setup(_ => _.IamVirtual()).Throws(new ArgumentOutOfRangeException("test exception"));
            var provider = new Mock<IAuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<InterceptMe>(realMocked.Object);
            var scope = AuditProxy.CurrentScope;
            Assert.Throws<ArgumentOutOfRangeException>(() => x.IamVirtual());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

    }
}
