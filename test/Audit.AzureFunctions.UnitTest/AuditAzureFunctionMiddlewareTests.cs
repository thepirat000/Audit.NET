using Audit.Core;
using Audit.Core.Providers;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Middleware;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Audit.AzureFunctions.ConfigurationApi;

namespace Audit.AzureFunctions.UnitTest;

/// <summary>
/// Unit Tests for <see cref="AuditAzureFunctionMiddleware"/>.
/// Note:
/// Integration tests are not provided since, WebApplicationFactory can't be used against isolated worker function apps.
/// See: https://github.com/Azure/azure-functions-dotnet-worker/issues/281
/// </summary>

[TestFixture]
public class AuditAzureFunctionMiddlewareTests
{
    [SetUp]
    public void SetUp()
    {
        Configuration.Reset();
    }

    [Test]
    public void Constructor_NoParameters_UseDefaultOptions()
    {
        var middleware = new AuditAzureFunctionMiddleware();

        Assert.Multiple(() =>
        {
            Assert.That(middleware.Options, Is.Not.Null);
            Assert.That(middleware.Options.DataProvider, Is.Null);
            Assert.That(middleware.Options.EventCreationPolicy, Is.Null);
        });
    }

    [Test]
    public void Constructor_OptionsParameters_UseGivenOptions()
    {
        var options = new AuditAzureFunctionOptions()
        {
            DataProvider = _ => new InMemoryDataProvider(),
            EventCreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd
        };

        var middleware = new AuditAzureFunctionMiddleware(options);
        Assert.That(middleware.Options, Is.SameAs(options));
    }

    [Test]
    public async Task Invoke_ShortCircuits_When_AuditDisabled_By_Options()
    {
        var dp = new InMemoryDataProvider();

        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.AuditWhen(_ => false);
        });

        var ctx = CreateFunctionContext().Object;

        bool nextCalled = false;
        var next = CreateNext(() => nextCalled = true);

        await middleware.Invoke(ctx, next);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(ctx.Items.ContainsKey(AuditAzureFunctionMiddleware.AuditEventKey), Is.False);
            Assert.That(dp.GetAllEventsOfType<AuditEvent>().Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Invoke_ShortCircuits_When_GlobalAuditDisabled_By_Options()
    {
        var dp = new InMemoryDataProvider();

        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.AuditWhen(_ => true);
        });

        var ctx = CreateFunctionContext().Object;

        bool nextCalled = false;
        var next = CreateNext(() => nextCalled = true);

        Configuration.AuditDisabled = true;

        await middleware.Invoke(ctx, next);

        Configuration.AuditDisabled = false;

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(ctx.Items.ContainsKey(AuditAzureFunctionMiddleware.AuditEventKey), Is.False);
            Assert.That(dp.GetAllEventsOfType<AuditEvent>().Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Invoke_GeneratesAuditEvent_Default()
    {
        var dp = new InMemoryDataProvider();
        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.IncludeFunctionDefinition();
            cfg.IncludeTriggerInfo();
            cfg.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
            cfg.WithCustomFields(_ => new Dictionary<string, object> { ["CF1"] = "V1" });
        });

        var triggerAttr = new DummyTriggerAttribute();
        var ctxMock = CreateFunctionContext(functionName: "MyFunc", triggerAttribute: triggerAttr);
        var ctx = ctxMock.Object;

        bool nextCalled = false;
        var next = CreateNext(() => nextCalled = true);

        await middleware.Invoke(ctx, next);

        // One event saved on end
        var events = dp.GetAllEventsOfType<AuditEventAzureFunction>();
        Assert.That(events, Is.Not.Null);
        Assert.That(events.Count, Is.EqualTo(1));
        var ev = events[0];

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(ctx.Items.ContainsKey(AuditAzureFunctionMiddleware.AuditEventKey), Is.True);

            Assert.That(ev.EventType, Is.EqualTo("MyFunc"));

            Assert.That(ev.Call.FunctionId, Is.EqualTo("FUNC-1"));
            Assert.That(ev.Call.InvocationId, Is.EqualTo("INV-1"));

            Assert.That(ev.Call.FunctionDefinition, Is.Not.Null);
            Assert.That(ev.Call.FunctionDefinition.Name, Is.EqualTo("MyFunc"));
            Assert.That(ev.Call.FunctionDefinition.Id, Is.EqualTo("DEF-1"));
            Assert.That(ev.Call.FunctionDefinition.EntryPoint, Is.EqualTo("Namespace.Type.Method"));
            Assert.That(ev.Call.FunctionDefinition.Assembly, Is.EqualTo("C:\\bin\\app.dll"));
            Assert.That(ev.Call.FunctionDefinition.Parameters.Count, Is.EqualTo(1));
            Assert.That(ev.Call.FunctionDefinition.Parameters[0].Name, Is.EqualTo("param1"));
            Assert.That(ev.Call.FunctionDefinition.Parameters[0].Type, Is.EqualTo(typeof(string).Name));
            Assert.That(ev.Call.FunctionDefinition.InputBindings.Count, Is.EqualTo(1));
            Assert.That(ev.Call.FunctionDefinition.InputBindings[0].Name, Is.EqualTo("input1"));
            Assert.That(ev.Call.FunctionDefinition.InputBindings[0].Type, Is.EqualTo("queueTrigger"));
            Assert.That(ev.Call.FunctionDefinition.OutputBindings.Count, Is.EqualTo(1));
            Assert.That(ev.Call.FunctionDefinition.OutputBindings[0].Name, Is.EqualTo("out1"));
            Assert.That(ev.Call.FunctionDefinition.OutputBindings[0].Type, Is.EqualTo("http"));

            Assert.That(ev.Call.BindingData["key1"], Is.EqualTo("val1"));
            Assert.That(ev.Call.BindingData["key2"], Is.EqualTo(42));

            Assert.That(ev.Call.Trace.TraceParent, Is.Not.Null.And.Not.Empty);

            Assert.That(ev.Call.Trigger, Is.Not.Null);
            Assert.That(ev.Call.Trigger.Type, Is.EqualTo("DummyTrigger"));
            Assert.That(ev.Call.Trigger.Attributes["Name"], Is.EqualTo("dummyName"));
            Assert.That(ev.Call.Trigger.Attributes["Value"], Is.EqualTo(123));
            Assert.That(ev.Call.Trigger.Attributes.ContainsKey("Throws"), Is.True);
            Assert.That(ev.Call.Trigger.Attributes["Throws"], Is.Null);

            Assert.That(ev.Call.CustomFields["CF1"], Is.EqualTo("V1"));

            Assert.That(ev.Call.Exception, Is.Null);
        });
    }

    [Test]
    public async Task Invoke_GeneratesAuditEvent_NoTrigger()
    {
        var dp = new InMemoryDataProvider();
        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.IncludeFunctionDefinition();
            cfg.IncludeTriggerInfo();
            cfg.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
            cfg.WithCustomFields(_ => new Dictionary<string, object> { ["CF1"] = "V1" });
        });

        var ctxMock = CreateFunctionContext(functionName: "MyFunc", triggerAttribute: null);
        var ctx = ctxMock.Object;

        bool nextCalled = false;
        var next = CreateNext(() => nextCalled = true);

        await middleware.Invoke(ctx, next);

        // One event saved on end
        var events = dp.GetAllEventsOfType<AuditEventAzureFunction>();
        Assert.That(events, Is.Not.Null);
        Assert.That(events.Count, Is.EqualTo(1));
        var ev = events[0];

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(ctx.Items.ContainsKey(AuditAzureFunctionMiddleware.AuditEventKey), Is.True);

            Assert.That(ev.EventType, Is.EqualTo("MyFunc"));

            Assert.That(ev.Call.FunctionId, Is.EqualTo("FUNC-1"));
            Assert.That(ev.Call.InvocationId, Is.EqualTo("INV-1"));

            Assert.That(ev.Call.FunctionDefinition, Is.Not.Null);
            Assert.That(ev.Call.FunctionDefinition.Name, Is.EqualTo("MyFunc"));
            Assert.That(ev.Call.FunctionDefinition.Id, Is.EqualTo("DEF-1"));
            Assert.That(ev.Call.FunctionDefinition.EntryPoint, Is.EqualTo("Namespace.Type.Method"));
            Assert.That(ev.Call.FunctionDefinition.Assembly, Is.EqualTo("C:\\bin\\app.dll"));
            Assert.That(ev.Call.FunctionDefinition.Parameters.Count, Is.EqualTo(1));
            Assert.That(ev.Call.FunctionDefinition.Parameters[0].Name, Is.EqualTo("param1"));
            Assert.That(ev.Call.FunctionDefinition.Parameters[0].Type, Is.EqualTo(typeof(string).Name));
            Assert.That(ev.Call.FunctionDefinition.InputBindings.Count, Is.EqualTo(1));
            Assert.That(ev.Call.FunctionDefinition.InputBindings[0].Name, Is.EqualTo("input1"));
            Assert.That(ev.Call.FunctionDefinition.InputBindings[0].Type, Is.EqualTo("queueTrigger"));
            Assert.That(ev.Call.FunctionDefinition.OutputBindings.Count, Is.EqualTo(1));
            Assert.That(ev.Call.FunctionDefinition.OutputBindings[0].Name, Is.EqualTo("out1"));
            Assert.That(ev.Call.FunctionDefinition.OutputBindings[0].Type, Is.EqualTo("http"));
            Assert.That(ev.Call.BindingData["key1"], Is.EqualTo("val1"));
            Assert.That(ev.Call.BindingData["key2"], Is.EqualTo(42));
            Assert.That(ev.Call.Trace.TraceParent, Is.Not.Null.And.Not.Empty);
            Assert.That(ev.Call.Trigger, Is.Null);
            Assert.That(ev.Call.CustomFields["CF1"], Is.EqualTo("V1"));
            Assert.That(ev.Call.Exception, Is.Null);
        });
    }

    [Test]
    public async Task Invoke_EventType_From_Options_Delegate_Takes_Precedence()
    {
        var dp = new InMemoryDataProvider();
        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
            cfg.EventType(ctx => $"exec-{ctx.FunctionDefinition.Name}-{ctx.FunctionId}");
        });

        var ctx = CreateFunctionContext(functionId: "F-XYZ", functionName: "Fn").Object;

        var next = CreateNext();
        await middleware.Invoke(ctx, next);

        var events = dp.GetAllEventsOfType<AuditEventAzureFunction>();
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].EventType, Is.EqualTo("exec-Fn-F-XYZ"));
    }

    [Test]
    public void Invoke_Captures_Exception_And_Rethrows()
    {
        var dp = new InMemoryDataProvider();
        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
        });

        var ctx = CreateFunctionContext().Object;

        var expected = new InvalidOperationException("boom");
        var next = CreateNext(throwEx: expected);

        var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () => await middleware.Invoke(ctx, next));
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown.Message, Is.EqualTo("boom"));

        var events = dp.GetAllEventsOfType<AuditEventAzureFunction>();
        Assert.That(events.Count, Is.EqualTo(1));
        var ev = events[0];

        Assert.Multiple(() =>
        {
            Assert.That(ev.Call.Exception, Does.Contain("boom"));
        });
    }

    [Test]
    public async Task CreateAuditEvent_Respects_Include_FunctionDefinition_And_Trigger_Flags()
    {
        var dp = new InMemoryDataProvider();
        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(_ => dp);
            cfg.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
            cfg.IncludeFunctionDefinition(_ => false);
            cfg.IncludeTriggerInfo(_ => false);
        });

        var triggerAttr = new DummyTriggerAttribute();
        var ctx = CreateFunctionContext(triggerAttribute: triggerAttr).Object;

        var next = CreateNext();
        await middleware.Invoke(ctx, next);

        var events = dp.GetAllEventsOfType<AuditEventAzureFunction>();
        Assert.That(events.Count, Is.EqualTo(1));
        var ev = events[0];

        Assert.Multiple(() =>
        {
            Assert.That(ev.Call.FunctionDefinition, Is.Null);
            Assert.That(ev.Call.Trigger, Is.Null);
        });
    }

    [Test]
    public async Task CreateAuditEvent_FunctionDefinition_Bindings_Null_Are_Handled()
    {
        var dp = new InMemoryDataProvider();
        var middleware = new AuditAzureFunctionMiddleware(cfg =>
        {
            cfg.DataProvider(dp);
            cfg.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
            cfg.IncludeFunctionDefinition();
        });

        var ctx = CreateFunctionContext(includeParamsAndBindings: false).Object;

        var next = CreateNext();
        await middleware.Invoke(ctx, next);

        var events = dp.GetAllEventsOfType<AuditEventAzureFunction>();
        Assert.That(events.Count, Is.EqualTo(1));
        var ev = events[0];

        Assert.Multiple(() =>
        {
            Assert.That(ev.Call.FunctionDefinition, Is.Not.Null);
            Assert.That(ev.Call.FunctionDefinition.InputBindings, Is.Null);
            Assert.That(ev.Call.FunctionDefinition.OutputBindings, Is.Null);
            Assert.That(ev.Call.FunctionDefinition.Parameters.Count, Is.EqualTo(1));
        });
    }

    #region Helper

    [AttributeUsage(AttributeTargets.All)]
    private class DummyTriggerAttribute : BindingAttribute
    {
        public string Name { get; set; } = "dummyName";
        public int Value { get; set; } = 123;
        public string Throws
        {
            get { throw new InvalidOperationException("getter failed"); }
        }
    }

    private static Mock<FunctionContext> CreateFunctionContext(
        string functionId = "FUNC-1",
        string invocationId = "INV-1",
        string functionName = "MyFunc",
        IReadOnlyDictionary<string, object> bindingData = null,
        IReadOnlyDictionary<string, string> traceAttributes = null,
        BindingAttribute triggerAttribute = null,
        bool includeParamsAndBindings = true)
    {
        var ctx = new Mock<FunctionContext>();

        ctx.SetupGet(c => c.FunctionId).Returns(functionId);
        ctx.SetupGet(c => c.InvocationId).Returns(invocationId);

        // Items
        var items = new Dictionary<object, object>();
        ctx.SetupGet(c => c.Items).Returns(items);

        // BindingContext
        var bindingCtx = new Mock<BindingContext>();
        bindingCtx.SetupGet(b => b.BindingData).Returns(bindingData ?? new Dictionary<string, object>
        {
            ["key1"] = "val1",
            ["key2"] = 42
        });
        ctx.SetupGet(c => c.BindingContext).Returns(bindingCtx.Object);

        // TraceContext
        var traceCtx = new Mock<TraceContext>();
        traceCtx.SetupGet(t => t.TraceParent).Returns("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00");
        traceCtx.SetupGet(t => t.Attributes).Returns(traceAttributes);
        ctx.SetupGet(c => c.TraceContext).Returns(traceCtx.Object);

        // FunctionDefinition
        var fdef = new Mock<FunctionDefinition>();
        fdef.SetupGet(f => f.Name).Returns(functionName);
        fdef.SetupGet(f => f.Id).Returns("DEF-1");
        fdef.SetupGet(f => f.EntryPoint).Returns("Namespace.Type.Method");
        fdef.SetupGet(f => f.PathToAssembly).Returns("C:\\bin\\app.dll");

        // Parameters with optional trigger attribute inside properties
        var props = new Dictionary<string, object>();
        if (triggerAttribute != null)
        {
            props["bindingAttribute"] = triggerAttribute;
        }
        var param = new FunctionParameter("param1", typeof(string), props);

        var parameters = new List<FunctionParameter> { param }.ToImmutableArray();

        fdef.SetupGet(f => f.Parameters).Returns(parameters);

        // Bindings
        if (includeParamsAndBindings)
        {
            var inputBinding = new Mock<BindingMetadata>();
            inputBinding.SetupGet(b => b.Type).Returns("queueTrigger");

            var outputBinding = new Mock<BindingMetadata>();
            outputBinding.SetupGet(b => b.Type).Returns("http");

            var inputBindings = new Dictionary<string, BindingMetadata> { ["input1"] = inputBinding.Object };
            var outputBindings = new Dictionary<string, BindingMetadata> { ["out1"] = outputBinding.Object };

            fdef.SetupGet(f => f.InputBindings).Returns(inputBindings.ToImmutableDictionary);
            fdef.SetupGet(f => f.OutputBindings).Returns(outputBindings.ToImmutableDictionary);
        }
        else
        {
            fdef.SetupGet(f => f.InputBindings).Returns((IImmutableDictionary<string, BindingMetadata>)null);
            fdef.SetupGet(f => f.OutputBindings).Returns((IImmutableDictionary<string, BindingMetadata>)null);
        }

        ctx.SetupGet(c => c.FunctionDefinition).Returns(fdef.Object);

        // No DI services: force middleware to use options-provided data provider
        ctx.SetupGet(c => c.InstanceServices).Returns((IServiceProvider)null);

        return ctx;
    }

    private static FunctionExecutionDelegate CreateNext(Action onInvoke = null, Exception throwEx = null)
    {
        return async _ =>
        {
            onInvoke?.Invoke();
            await Task.Yield();
            if (throwEx != null)
            {
                throw throwEx;
            }
        };
    }

    #endregion
}