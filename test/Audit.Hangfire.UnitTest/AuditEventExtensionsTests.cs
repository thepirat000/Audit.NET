using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

namespace Audit.Hangfire.UnitTest;

[TestFixture]
public class AuditEventExtensionsTests
{
    [SetUp]
    public void SetUp()
    {
        Configuration.Reset();
        Configuration.Setup().UseInMemoryProvider();
    }

    [Test]
    public void GetHangfireJobCreationEvent_From_AuditEvent_Returns_JobCreation()
    {
        var creation = new HangfireJobCreationEvent { JobId = "J1" };
        var ev = new AuditEventHangfireJobCreation { JobCreation = creation };

        var result = ev.GetHangfireJobCreationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JobId, Is.EqualTo("J1"));
        });
    }

    [Test]
    public void GetHangfireJobCreationEvent_From_AuditEvent_Returns_Null_For_WrongType()
    {
        var ev = new AuditEventHangfireJobExecution { JobExecution = new HangfireJobExecutionEvent { JobId = "X" } };

        var result = ev.GetHangfireJobCreationEvent();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetHangfireJobCreationEvent_From_AuditScope_Returns_JobCreation()
    {
        var creation = new HangfireJobCreationEvent { JobId = "S1" };
        var ev = new AuditEventHangfireJobCreation { JobCreation = creation };

        using var scope = AuditScope.Create(new AuditScopeOptions
        {
            AuditEvent = ev,
            EventType = "test",
            CreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = new InMemoryDataProvider()
        });

        var result = scope.GetHangfireJobCreationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JobId, Is.EqualTo("S1"));
        });
    }

    [Test]
    public void GetHangfireJobCreationEvent_From_AuditScope_Returns_Null_For_WrongType()
    {
        var ev = new AuditEventHangfireJobExecution { JobExecution = new HangfireJobExecutionEvent { JobId = "E2" } };

        using var scope = AuditScope.Create(new AuditScopeOptions
        {
            AuditEvent = ev,
            EventType = "test",
            CreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = new InMemoryDataProvider()
        });

        var result = scope.GetHangfireJobCreationEvent();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetHangfireJobExecutionEvent_From_AuditEvent_Returns_JobExecution()
    {
        var execution = new HangfireJobExecutionEvent { JobId = "J2" };
        var ev = new AuditEventHangfireJobExecution { JobExecution = execution };

        var result = ev.GetHangfireJobExecutionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JobId, Is.EqualTo("J2"));
        });
    }

    [Test]
    public void GetHangfireJobExecutionEvent_From_AuditEvent_Returns_Null_For_WrongType()
    {
        var ev = new AuditEventHangfireJobCreation { JobCreation = new HangfireJobCreationEvent { JobId = "C" } };

        var result = ev.GetHangfireJobExecutionEvent();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetHangfireJobExecutionEvent_From_AuditScope_Returns_JobExecution()
    {
        var execution = new HangfireJobExecutionEvent { JobId = "S2" };
        var ev = new AuditEventHangfireJobExecution { JobExecution = execution };

        using var scope = AuditScope.Create(new AuditScopeOptions
        {
            AuditEvent = ev,
            EventType = "test",
            CreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = new InMemoryDataProvider()
        });

        var result = scope.GetHangfireJobExecutionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JobId, Is.EqualTo("S2"));
        });
    }

    [Test]
    public void GetHangfireJobExecutionEvent_From_AuditScope_Returns_Null_For_WrongType()
    {
        var ev = new AuditEventHangfireJobCreation { JobCreation = new HangfireJobCreationEvent { JobId = "C2" } };

        using var scope = AuditScope.Create(new AuditScopeOptions
        {
            AuditEvent = ev,
            EventType = "test",
            CreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = new InMemoryDataProvider()
        });

        var result = scope.GetHangfireJobExecutionEvent();

        Assert.That(result, Is.Null);
    }
}