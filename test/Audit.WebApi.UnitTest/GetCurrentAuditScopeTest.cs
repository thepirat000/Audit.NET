#if NET45
using NUnit.Framework;
using System;
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
            var sut = new TestController();

            Action act = () => sut.TestAction();

            Assert.DoesNotThrow(new TestDelegate(act));
        }
    }
}
#endif