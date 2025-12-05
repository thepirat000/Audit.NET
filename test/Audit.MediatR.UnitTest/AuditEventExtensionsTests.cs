using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

namespace Audit.MediatR.UnitTest;

[TestFixture]
public class AuditEventExtensionsTests
{
    [Test]
    public void GetMediatRCallAction_From_AuditScope_Returns_Null_If_Null_Scope()
    {
        AuditScope auditScope = null;

        var result = auditScope.GetMediatRCallAction();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMediatRCallAction_From_AuditEvent_Returns_Null_If_Not_AuditEventMediatR()
    {
        var auditEvent = new AuditEvent();

        var result = auditEvent.GetMediatRCallAction();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMediatRCallAction_From_AuditEvent_Returns_Call_When_AuditEventMediatR()
    {
        var expectedCall = new MediatRCallAction();

        var auditEvent = new AuditEventMediatR
        {
            Call = expectedCall
        };

        var result = auditEvent.GetMediatRCallAction();

        Assert.That(result, Is.SameAs(expectedCall));
    }

    [Test]
    public void GetMediatRCallAction_From_AuditScope_Returns_Call_When_AuditEventMediatR()
    {
        var expectedCall = new MediatRCallAction();

        var auditEvent = new AuditEventMediatR
        {
            Call = expectedCall
        };

        using var auditScope = AuditScope.Create(new AuditScopeOptions(c => c.DataProvider(new NullDataProvider()).AuditEvent(auditEvent)));

        var result = auditScope.GetMediatRCallAction();

        Assert.That(result, Is.SameAs(expectedCall));
    }
}