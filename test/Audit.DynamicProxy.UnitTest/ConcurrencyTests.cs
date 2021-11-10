using Audit.Core;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.DynamicProxy.UnitTest
{
    public class ConcurrencyTests
    {
        [Test]
        public void Test_Concurrency()
        {
            var provider = new Mock<AuditDataProvider>();
            var real = new InterceptTest();
            var intercepted = AuditProxy.Create(real, new InterceptionSettings() { AuditDataProvider = provider.Object });
            intercepted.M1(Guid.NewGuid());

            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                var t1 = Task.Factory.StartNew(() => intercepted.M1(Guid.NewGuid()));
                var t2 = Task.Factory.StartNew(() => intercepted.M2(Guid.NewGuid()));
                var t3 = Task.Factory.StartNew(() => intercepted.M3(Guid.NewGuid()));
                tasks.AddRange(new[] { t1, t2, t3 });
            }
            Task.WaitAll(tasks.ToArray());
        }
    }

    public class InterceptTest
    {
        public virtual void M1(Guid i)
        {
            var rnd = new Random();
            var scope = AuditProxy.CurrentScope;

            var th = new System.Threading.Thread(() =>
            {
                var innerscope = AuditProxy.CurrentScope;
                Assert.Null(innerscope);
            });
            th.Start();
            th.Join();
            Assert.AreEqual("M1", scope.Event.GetAuditInterceptEvent().MethodName);
            Assert.AreEqual(i, scope.Event.GetAuditInterceptEvent().Arguments[0].Value);
        }

        public virtual void M2(Guid i)
        {
            var rnd = new Random();
            var scope = AuditProxy.CurrentScope;
            Assert.AreEqual("M2", scope.Event.GetAuditInterceptEvent().MethodName);
            Assert.AreEqual(i, scope.Event.GetAuditInterceptEvent().Arguments[0].Value);
        }
        public virtual void M3(Guid i)
        {
            var rnd = new Random();
            var scope = AuditProxy.CurrentScope;
            Assert.AreEqual("M3", scope.Event.GetAuditInterceptEvent().MethodName);
            Assert.AreEqual(i, scope.Event.GetAuditInterceptEvent().Arguments[0].Value);
        }
    }
}
