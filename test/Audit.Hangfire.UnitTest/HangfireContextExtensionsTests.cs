using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace Audit.Hangfire.UnitTest;

[TestFixture]
public class HangfireContextExtensionsTests
{
    private static CreatingContext CreateCreatingContextWithItems(Dictionary<string, object> items)
    {
        var storage = new Mock<JobStorage>();
        var connection = new Mock<IStorageConnection>();
        var job = new Job(typeof(DummyJob).GetMethod(nameof(DummyJob.Run)), "a", 1);
        var state = new Mock<IState>();
        var baseCtx = new CreateContext(storage.Object, connection.Object, job, state.Object);
        var ctx = new CreatingContext(baseCtx);
        foreach (var item in items)
        {
            ctx.Items[item.Key] = item.Value;
        }
        return ctx;
    }

    private static PerformContext CreatePerformContextWithItems(Dictionary<string, object> items)
    {
        var connection = new Mock<IStorageConnection>();
        var storage = new Mock<JobStorage>();
        var job = new Job(typeof(DummyJob).GetMethod(nameof(DummyJob.Run)), "a", 1);
        var bg = new BackgroundJob(Guid.NewGuid().ToString(), job, DateTime.UtcNow);
        var pc = new PerformContext(storage.Object, connection.Object, bg, new JobCancellationToken(false));
        var ctx = new PerformingContext(pc);
        foreach (var item in items)
        {
            ctx.Items[item.Key] = item.Value;
        }
        return ctx;
    }

    public class DummyJob
    {
        public void Run(string a, int b) { }
    }

    [Test]
    public void GetAuditEvent_From_CreateContext_Returns_Event_When_Present()
    {
        var ev = new AuditEventHangfireJobCreation
        {
            JobCreation = new HangfireJobCreationEvent { JobId = "C1" }
        };

        var items = new Dictionary<string, object>
        {
            { AuditJobCreationFilterAttribute.AuditEventKey, ev }
        };

        var ctx = CreateCreatingContextWithItems(items);

        var result = ctx.GetAuditEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JobCreation.JobId, Is.EqualTo("C1"));
        });
    }

    [Test]
    public void GetAuditEvent_From_CreateContext_Returns_Null_When_Not_Present_Or_WrongType()
    {
        var items = new Dictionary<string, object>(); // no key
        var ctx = CreateCreatingContextWithItems(items);

        var result1 = ctx.GetAuditEvent();
        Assert.That(result1, Is.Null);

        items[AuditJobCreationFilterAttribute.AuditEventKey] = new object(); // wrong type
        var result2 = ctx.GetAuditEvent();
        Assert.That(result2, Is.Null);
    }

    [Test]
    public void GetAuditEvent_From_PerformContext_Returns_Event_When_Present()
    {
        var ev = new AuditEventHangfireJobExecution
        {
            JobExecution = new HangfireJobExecutionEvent { JobId = "E1" }
        };

        var items = new Dictionary<string, object>
        {
            { AuditJobExecutionFilterAttribute.AuditEventKey, ev }
        };

        var ctx = CreatePerformContextWithItems(items);

        var result = ctx.GetAuditEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JobExecution.JobId, Is.EqualTo("E1"));
        });
    }

    [Test]
    public void GetAuditEvent_From_PerformContext_Returns_Null_When_Not_Present_Or_WrongType()
    {
        var items = new Dictionary<string, object>(); // no key
        var ctx = CreatePerformContextWithItems(items);

        var result1 = ctx.GetAuditEvent();
        Assert.That(result1, Is.Null);

        items[AuditJobExecutionFilterAttribute.AuditEventKey] = new object(); // wrong type
        var result2 = ctx.GetAuditEvent();
        Assert.That(result2, Is.Null);
    }
}