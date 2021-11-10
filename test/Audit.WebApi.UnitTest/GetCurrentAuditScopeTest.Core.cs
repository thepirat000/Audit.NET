﻿#if NETCOREAPP3_1 || NETCOREAPP1_0 || NETCOREAPP2_0 || NET451 || NET5_0
using Audit.Core;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
            var evs = new List<AuditEvent>();
            Configuration.Setup().UseDynamicProvider(_ => _.OnInsertAndReplace(ev => evs.Add(ev)));

            var sut = new TestController();
            Action act = () => sut.TestAction();

            Assert.DoesNotThrow(new TestDelegate(act));
            Assert.AreEqual(0, evs.Count);
        }
    }
}
#endif