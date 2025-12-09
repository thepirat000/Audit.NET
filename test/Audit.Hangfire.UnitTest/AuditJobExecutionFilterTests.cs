using Audit.Core;
using Audit.Core.Providers;
using Audit.Hangfire.ConfigurationApi;

using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

using Moq;

using NUnit.Framework;

using System;
using System.Reflection;

namespace Audit.Hangfire.UnitTest;

[TestFixture]
public class AuditJobExecutionFilterTests
{
    [SetUp]
    public void SetUp()
    {
        Configuration.Reset();
        Configuration.Setup().UseInMemoryProvider();
    }

    private static Job CreateDummyJob()
    {
        var method = typeof(DummyJob).GetMethod(nameof(DummyJob.Run), BindingFlags.Public | BindingFlags.Instance);
        return new Job(typeof(DummyJob), method, "arg1", 2);
    }

    private static PerformingContext GetDummyPerformingContext(string jobId = null, object[] args = null)
    {
        var job = args == null ? CreateDummyJob() : new Job(typeof(DummyJob), typeof(DummyJob).GetMethod(nameof(DummyJob.Run)), args);
        var bgJob = new BackgroundJob(jobId ?? Guid.NewGuid().ToString(), job, DateTime.UtcNow);
        var storage = new Mock<JobStorage>();
        var connection = new Mock<IStorageConnection>();
        var performContext = new PerformContext(storage.Object, connection.Object, bgJob, new JobCancellationToken(false));
        var performCtx = new PerformingContext(performContext);
        return performCtx;
    }

    private static PerformedContext GetDummyPerformedContextFrom(PerformingContext performingContext, object result = null, Exception ex = null, bool canceled = false)
    {
        var performed = new PerformedContext(performingContext, result, canceled, ex);
        return performed;
    }

    public class DummyJob
    {
        public void Run(string a, int b) { }
    }

    [Test]
    public void Execution_OnPerforming_ShortCircuits_When_AuditDisabled_By_Options()
    {
        // Arrange
        var attr = new AuditJobExecutionFilterAttribute(new AuditJobExecutionOptions { AuditWhen = _ => false });
        var performingCtx = GetDummyPerformingContext();

        // Act
        attr.OnPerforming(performingCtx);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(performingCtx.Items.ContainsKey(AuditJobExecutionFilterAttribute.AuditEventKey), Is.False);
            Assert.That(performingCtx.Items.ContainsKey(AuditJobExecutionFilterAttribute.AuditScopeKey), Is.False);
        });
    }

    [Test]
    public void Execution_OnPerforming_Stores_AuditEvent_And_Scope()
    {
        // Arrange
        var dp = new InMemoryDataProvider();

        var options = new AuditJobExecutionOptions
        {
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = _ => dp
        };

        var attr = new AuditJobExecutionFilterAttribute(options);
        var performingCtx = GetDummyPerformingContext(args: new object[] { "A", 7 });

        // Act
        attr.OnPerforming(performingCtx);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(performingCtx.Items.ContainsKey(AuditJobExecutionFilterAttribute.AuditEventKey), Is.True);
            Assert.That(performingCtx.Items.ContainsKey(AuditJobExecutionFilterAttribute.AuditScopeKey), Is.True);
        });
    }

    [Test]
    public void Execution_OnPerformed_Updates_Event_And_Disposes_Scope()
    {
        // Arrange
        var dp = new InMemoryDataProvider();

        var options = new AuditJobExecutionOptions
        {
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = _ => dp
        };

        var attr = new AuditJobExecutionFilterAttribute(options);

        var performingCtx = GetDummyPerformingContext(jobId: "E-123");
        attr.OnPerforming(performingCtx);

        var performedCtx = GetDummyPerformedContextFrom(performingCtx, result: "OK", ex: null, canceled: false);

        // Act
        attr.OnPerformed(performedCtx);

        // Assert
        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobExecution>();
        Assert.That(evs.Count, Is.EqualTo(1));
        var ev = evs[0];

        Assert.Multiple(() =>
        {
            Assert.That(ev.JobExecution.JobId, Is.EqualTo("E-123"));
            Assert.That(ev.JobExecution.Result, Is.EqualTo("OK"));
            Assert.That(ev.JobExecution.Exception, Is.Null);
            Assert.That(ev.JobExecution.Canceled, Is.False);
            Assert.That(performedCtx.Items.ContainsKey(AuditJobExecutionFilterAttribute.AuditScopeKey), Is.False);
        });
    }

    
    [Test]
    public void Execution_AreArgumentsExcluded_Respects_Options_Delegate()
    {
        // Arrange
        var attr = new AuditJobExecutionFilterAttribute(new AuditJobExecutionOptions { ExcludeArguments = _ => true });
        var performingCtx = GetDummyPerformingContext();

        // Act / Assert
        Assert.That(attr.AreArgumentsExcluded(performingCtx), Is.True);
    }

    [Test]
    public void Execution_EventType_From_Options_Delegate_Takes_Precendence()
    {
        // Arrange
        var dp = new InMemoryDataProvider();

        var attr = new AuditJobExecutionFilterAttribute(new AuditJobExecutionOptions
        {
            DataProvider = _ => dp,
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd,
            EventType = ctx => $"exec-{ctx.BackgroundJob.Job.Type.Name}-{ctx.BackgroundJob.Job.Method.Name}"
        })
        {
            EventType = "{type}.{method}" // ignored due to options delegate
        };

        var performingCtx = GetDummyPerformingContext();

        // Act
        attr.OnPerforming(performingCtx);

        // Dispose via OnPerformed to persist
        var performedCtx = GetDummyPerformedContextFrom(performingCtx);
        attr.OnPerformed(performedCtx);

        // Assert
        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobExecution>();
        Assert.That(evs.Count, Is.EqualTo(1));
        Assert.That(evs[0].EventType, Is.EqualTo($"exec-{nameof(DummyJob)}-{nameof(DummyJob.Run)}"));
    }

    [Test]
    public void Execution_Args_Included_When_Not_Excluded()
    {
        // Arrange
        var dp = new InMemoryDataProvider();
        var attr = new AuditJobExecutionFilterAttribute(new AuditJobExecutionOptions
        {
            DataProvider = _ => dp,
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd
        })
        {
            ExcludeArguments = false
        };

        var performingCtx = GetDummyPerformingContext(args: new object[] { "argX", 99 });

        // Act
        attr.OnPerforming(performingCtx);
        var performedCtx = GetDummyPerformedContextFrom(performingCtx);
        attr.OnPerformed(performedCtx);

        // Assert
        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobExecution>();
        Assert.That(evs.Count, Is.EqualTo(1));
        Assert.That(evs[0].JobExecution.Args, Has.Count.EqualTo(2));
        Assert.That(evs[0].JobExecution.Args[0], Is.EqualTo("argX"));
        Assert.That(evs[0].JobExecution.Args[1], Is.EqualTo(99));
    }

    [Test]
    public void Execution_Args_Excluded_When_Option_True()
    {
        // Arrange
        var dp = new InMemoryDataProvider();
        var attr = new AuditJobExecutionFilterAttribute(new AuditJobExecutionOptions
        {
            DataProvider = _ => dp,
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd,
            ExcludeArguments = _ => true
        });

        var performingCtx = GetDummyPerformingContext(args: new object[] { "argX", 99 });

        // Act
        attr.OnPerforming(performingCtx);
        var performedCtx = GetDummyPerformedContextFrom(performingCtx);
        attr.OnPerformed(performedCtx);

        // Assert
        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobExecution>();
        Assert.That(evs.Count, Is.EqualTo(1));
        Assert.That(evs[0].JobExecution.Args, Is.Null);
    }
    
    [Test]
    public void Execution_IsAuditDisabled_Global_And_Option()
    {
        var attr = new AuditJobExecutionFilterAttribute(new AuditJobExecutionOptions { AuditWhen = _ => false });
        var ctx = GetDummyPerformingContext();

        Assert.That(attr.IsAuditDisabled(ctx), Is.True);

        Configuration.AuditDisabled = true;
        Assert.That(attr.IsAuditDisabled(ctx), Is.True);
        Configuration.AuditDisabled = false;
    }
}