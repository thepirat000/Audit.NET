#if NETCOREAPP1_0 || NETCOREAPP2_0 || NET451
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;

namespace Audit.WebApi.UnitTest
{
    public class TestController : ControllerBase
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