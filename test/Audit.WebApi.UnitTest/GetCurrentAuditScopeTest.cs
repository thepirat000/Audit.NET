#if NET45
using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Audit.WebApi.UnitTest
{
    public class TestController : ApiController
    {
        public void TestAction()
        {
            var scope = this.GetCurrentAuditScope();
            scope.SetCustomField("Field", 1);
            scope.Save();
        }
    }

    public class GetCurrentAuditScopeTest
    {
        [Test]
        public void Test_CallingAnAction_ShouldNotThrow()
        {
            var evs = new List<AuditEvent>();
            Configuration.Setup().UseDynamicProvider(_ => _.OnInsertAndReplace(ev => evs.Add(ev)));

            var sut = new TestController();
            Action act = () => sut.TestAction();

            Assert.DoesNotThrow(new TestDelegate(act));
        }
    }
}
#endif