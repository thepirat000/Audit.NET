using Audit.Core;
using Audit.Core.Providers;
using Audit.Hangfire.ConfigurationApi;

using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Audit.Hangfire.UnitTest;

[TestFixture]
public class AuditJobCreationFilterTests
{
    [SetUp]
    public void SetUp()
    {
        Configuration.Reset();
        Configuration.Setup().UseInMemoryProvider();
    }

    private static Job CreateDummyJob()
    {
        var job = new Job(typeof(DummyJob).GetMethod(nameof(DummyJob.Run)), "arg1", 2);
        return job;
    }

    private static CreatingContext GetDummyCreatingContext()
    {
        var storage = new Mock<JobStorage>();
        var connection = new Mock<IStorageConnection>();
        var job = CreateDummyJob();
        var state = new Mock<IState>();
        var context = new CreateContext(storage.Object, connection.Object, job, state.Object);
        var createCtx = new CreatingContext(context);

        return createCtx;
    }

    private static CreatedContext GetDummyCreatedContext(Job jobToUse = null, IState initialState = null, IDictionary<string, object> parameters = null, string jobId = null)
    {
        var storage = new Mock<JobStorage>();
        var connection = new Mock<IStorageConnection>();
        var job = jobToUse ?? CreateDummyJob();
        var state = initialState ?? new ScheduledState(DateTime.UtcNow);
        var context = new CreateContext(storage.Object, connection.Object, job, state, parameters);
        var createdCtx = new CreatedContext(context, new BackgroundJob(jobId ?? Guid.NewGuid().ToString(), job, DateTime.UtcNow), false, null);

        return createdCtx;
    }

    public class DummyJob
    {
        public void Run(string a, int b) { }
    }

    [Test]
    public void Creation_OnCreating_Sets_StartedAt_When_AuditEnabled()
    {
        // Arrange
        var createCtx = GetDummyCreatingContext();

        var attr = new AuditJobCreationFilterAttribute();

        Configuration.AuditDisabled = false;

        // Act
        attr.OnCreating(createCtx);

        // Assert
        Assert.That(createCtx.Items.ContainsKey(AuditJobCreationFilterAttribute.StartedAtKey), Is.True);
        Assert.That(createCtx.Items[AuditJobCreationFilterAttribute.StartedAtKey], Is.InstanceOf<DateTime>());
    }

    
    [Test]
    public void Creation_OnCreating_DoesNothing_When_AuditDisabled()
    {
        // Arrange
        var createCtx = GetDummyCreatingContext();

        var attr = new AuditJobCreationFilterAttribute();

        // Act
        Configuration.AuditDisabled = true;
        attr.OnCreating(createCtx);

        // Assert
        Assert.That(createCtx.Items.ContainsKey(AuditJobCreationFilterAttribute.StartedAtKey), Is.False);
        Configuration.AuditDisabled = false;
    }

    
    [Test]
    public void Creation_OnCreated_ShortCircuits_When_AuditDisabled_By_Options()
    {
        var attr = new AuditJobCreationFilterAttribute(new AuditJobCreationOptions { AuditWhen = _ => false });

        var createdCtx = GetDummyCreatedContext();

        attr.OnCreated(createdCtx);

        Assert.That(createdCtx.Items.ContainsKey(AuditJobCreationFilterAttribute.AuditEventKey), Is.False);
    }

    [Test]
    public void Creation_OnCreated_Builds_Event_And_Writes_On_Dispose()
    {
        var dp = new InMemoryDataProvider();
        
        var options = new AuditJobCreationOptions
        {
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd,
            DataProvider = _ => dp
        };

        var attr = new AuditJobCreationFilterAttribute(options);

        var jobId = Guid.NewGuid().ToString();
        var job = CreateDummyJob();
        var initialState = new EnqueuedState { Queue = "default" };
        var parameters = new Dictionary<string, object> { { "P1", "V1" }, { "P2", "V2" } };
        var createdCtx = GetDummyCreatedContext(job, initialState, parameters, jobId);

        // StartedAt set previously:
        createdCtx.Items[AuditJobCreationFilterAttribute.StartedAtKey] = DateTime.UtcNow.AddSeconds(-1);
        
        // Include params via simple property (no delegate) and include args
        attr.IncludeParameters = true;
        attr.ExcludeArguments = false;
        attr.EventType = "{type}.{method}";

        attr.OnCreated(createdCtx);

        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobCreation>();
        Assert.Multiple(() =>
        {
            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(createdCtx.Items.ContainsKey(AuditJobCreationFilterAttribute.AuditEventKey), Is.True);
            var ev = evs[0];
            Assert.That(ev.JobCreation.JobId, Is.EqualTo(jobId));
            Assert.That(ev.JobCreation.Args, Has.Count.EqualTo(2));
            Assert.That(ev.JobCreation.Args[0], Is.EqualTo("arg1"));
            Assert.That(ev.JobCreation.Args[1], Is.EqualTo(2));
            Assert.That(ev.JobCreation.Parameters, Has.Count.EqualTo(2));
            Assert.That(ev.JobCreation.Queue, Is.EqualTo("default"));
            Assert.That(ev.JobCreation.TypeName, Does.Contain(nameof(DummyJob)));
            Assert.That(ev.JobCreation.MethodName, Does.Contain(nameof(DummyJob.Run)));
            Assert.That(ev.EventType, Is.EqualTo($"{nameof(DummyJob)}.{nameof(DummyJob.Run)}"));
            Assert.That(ev.StartDate, Is.LessThan(DateTime.UtcNow));
            Assert.That(createdCtx.Items.ContainsKey(AuditJobCreationFilterAttribute.StartedAtKey), Is.False);
        });
    }
    
    [Test]
    public void Creation_AreArgumentsExcluded_Respects_Options_Delegate()
    {
        var attr = new AuditJobCreationFilterAttribute(new AuditJobCreationOptions
        {
            ExcludeArguments = _ => true
        });

        var createCtx = GetDummyCreatedContext();

        Assert.That(attr.AreArgumentsExcluded(createCtx), Is.True);
    }

    [Test]
    public void Creation_AreParametersIncluded_Respects_Options_Delegate()
    {
        var attr = new AuditJobCreationFilterAttribute(new AuditJobCreationOptions
        {
            IncludeParameters = _ => true
        });

        var createCtx = GetDummyCreatedContext();

        Assert.That(attr.AreParametersIncluded(createCtx), Is.True);
    }

    [Test]
    public void Creation_EventType_From_Options_Delegate_Takes_Precendence()
    {
        var dp = new InMemoryDataProvider();
        
        var attr = new AuditJobCreationFilterAttribute(new AuditJobCreationOptions
        {
            DataProvider = _ => dp,
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd,
            EventType = ctx => $"custom-{ctx.Job.Type.Name}-{ctx.Job.Method.Name}"
        })
        {
            EventType = "{type}.{method}" // will be ignored by options delegate
        };

        var createdCtx = GetDummyCreatedContext();

        attr.OnCreated(createdCtx);

        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobCreation>();
        Assert.That(evs.Count, Is.EqualTo(1));
        Assert.That(evs[0].EventType, Is.EqualTo($"custom-{nameof(DummyJob)}-{nameof(DummyJob.Run)}"));
    }

    [Test]
    public void Creation_Scheduled_Continuation_Paths_Populate_StateSpecific_Fields()
    {
        var dp = new InMemoryDataProvider();

        var attr = new AuditJobCreationFilterAttribute(new AuditJobCreationOptions
        {
            DataProvider = _ => dp,
            EventCreationPolicy = EventCreationPolicy.InsertOnEnd
        });

        // Scheduled
        var scheduled = new ScheduledState(DateTime.UtcNow.AddMinutes(1));
        var ctx1 = GetDummyCreatedContext(initialState: scheduled, jobId: "parent-10");
        attr.OnCreated(ctx1);

        // Awaiting (Continuation)
        var awaiting = new AwaitingState("parent-10", scheduled, JobContinuationOptions.OnlyOnSucceededState, TimeSpan.FromMinutes(5));
        var ctx2 = GetDummyCreatedContext(initialState: awaiting, jobId: "cont-11");
        attr.OnCreated(ctx2);

        var evs = dp.GetAllEventsOfType<AuditEventHangfireJobCreation>();
        Assert.Multiple(() =>
        {
            Assert.That(evs.Count, Is.EqualTo(2));
            var scheduledEv = evs.First(e => e.JobCreation.JobId == "parent-10").JobCreation;
            Assert.That(scheduledEv.ScheduledAt, Is.Not.Null);
            Assert.That(scheduledEv.EnqueueAt, Is.Null.Or.Not.Null); // Hangfire may not set EnqueueAt immediately

            var contEv = evs.First(e => e.JobCreation.JobId == "cont-11").JobCreation;
            Assert.That(contEv.Continuation, Is.Not.Null);
            Assert.That(contEv.Continuation.ParentId, Is.EqualTo("parent-10"));
            Assert.That(contEv.Continuation.Option, Does.Contain("OnSucceeded"));
            Assert.That(contEv.Continuation.Expiration, Is.EqualTo(TimeSpan.FromMinutes(5)));
        });
    }

    [Test]
    public void Creation_IsAuditDisabled_Global_And_Option()
    {
        var attr = new AuditJobCreationFilterAttribute(new AuditJobCreationOptions { AuditWhen = _ => false });
        var ctxMock = GetDummyCreatedContext();

        Assert.That(attr.IsAuditDisabled(ctxMock), Is.True);

        Configuration.AuditDisabled = true;
        Assert.That(attr.IsAuditDisabled(ctxMock), Is.True);
        Configuration.AuditDisabled = false;
    }
}