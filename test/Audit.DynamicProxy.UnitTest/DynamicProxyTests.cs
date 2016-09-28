using Audit.Core;
using Moq;
using System;
using Xunit;

namespace Audit.DynamicProxy.UnitTest
{
    public class DynamicProxyTests
    {
        [Fact]
        public void Test_Config_DataProvider()
        {
            var real = new InterceptMe("test");
            var guid = Guid.NewGuid().ToString();
            var provider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);
            var x = AuditProxy.Create<IInterceptMe>(real, null);
            var str = x.ReturnString(guid);
            Assert.Equal(guid.ToUpper(), str);
            Assert.Equal("test", x.GetSomePropValue());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.InsertEvent(It.Is<AuditEvent>(ev => ev.GetAuditInterceptEvent().Arguments[0].Value == guid)), Times.Once);
        }

        [Fact]
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
            Assert.Equal("test", x.GetSomePropValue());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Fact]
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
            Assert.Equal("test", x.GetSomePropValue());
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Fact]
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
