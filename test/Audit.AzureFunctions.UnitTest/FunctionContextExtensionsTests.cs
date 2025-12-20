using Microsoft.Azure.Functions.Worker;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Audit.AzureFunctions.UnitTest;

public class FunctionContextExtensionsTests
{
    private static FunctionContext CreateContextWithItems(IDictionary<object, object> items)
    {
        var ctxMock = new Mock<FunctionContext>();
        ctxMock.SetupGet(c => c.Items).Returns(items);

        var fdef = new Mock<FunctionDefinition>();
        fdef.SetupGet(f => f.Name).Returns("functionName");
        fdef.SetupGet(f => f.Id).Returns("DEF-1");
        fdef.SetupGet(f => f.EntryPoint).Returns("Namespace.Type.Method");
        fdef.SetupGet(f => f.PathToAssembly).Returns("C:\\bin\\app.dll");

        // Parameters with optional trigger attribute inside properties
        var props = new Dictionary<string, object>();
        //props["bindingAttribute"] = new trigge;
        var param = new FunctionParameter("param1", typeof(string), props);
        var parameters = new List<FunctionParameter> { param }.ToImmutableArray();
        fdef.SetupGet(f => f.Parameters).Returns(parameters);
        var inputBinding = new Mock<BindingMetadata>();
        inputBinding.SetupGet(b => b.Type).Returns("queueTrigger");

        var outputBinding = new Mock<BindingMetadata>();
        outputBinding.SetupGet(b => b.Type).Returns("http");

        var inputBindings = new Dictionary<string, BindingMetadata> { ["input1"] = inputBinding.Object };
        var outputBindings = new Dictionary<string, BindingMetadata> { ["out1"] = outputBinding.Object };

        fdef.SetupGet(f => f.InputBindings).Returns(inputBindings.ToImmutableDictionary);
        fdef.SetupGet(f => f.OutputBindings).Returns(outputBindings.ToImmutableDictionary);

        ctxMock.SetupGet(c => c.FunctionDefinition).Returns(fdef.Object);

        return ctxMock.Object;
    }

    [Test]
    public void GetAuditEvent_ReturnsNull_WhenNoEventPresent()
    {
        var context = CreateContextWithItems(new Dictionary<object, object>());

        var result = context.GetAuditEvent();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAuditEvent_ReturnsEvent_WhenPresent()
    {
        var evt = new AuditEventAzureFunction();
        var items = new Dictionary<object, object>
        {
            { AuditAzureFunctionMiddleware.AuditEventKey, evt }
        };
        var context = CreateContextWithItems(items);

        var result = context.GetAuditEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(evt));
        });
    }

    [Test]
    public void GetAuditFunctionCall_ReturnsNull_WhenNoEventPresent()
    {
        var context = CreateContextWithItems(new Dictionary<object, object>());

        var call = context.GetAuditFunctionCall();

        Assert.That(call, Is.Null);
    }

    [Test]
    public void GetAuditFunctionCall_ReturnsCall_WhenEventHasCall()
    {
        var evt = new AuditEventAzureFunction
        {
            Call = new AzureFunctionCall()
        };
        var items = new Dictionary<object, object>
        {
            { AuditAzureFunctionMiddleware.AuditEventKey, evt }
        };
        var context = CreateContextWithItems(items);

        var call = context.GetAuditFunctionCall();

        Assert.Multiple(() =>
        {
            Assert.That(call, Is.Not.Null);
            Assert.That(call, Is.SameAs(evt.Call));
        });
    }

    [Test]
    public void GetAuditScope_ReturnsNull_WhenNoEventPresent()
    {
        var context = CreateContextWithItems(new Dictionary<object, object>());

        var scope = context.GetAuditScope();

        Assert.That(scope, Is.Null);
    }

    [Test]
    public void GetAuditScope_ReturnsNull_WhenEventScopeIsNull()
    {
        // Arrange: Event without scope (GetScope returns null by default)
        var evt = new AuditEventAzureFunction();
        var items = new Dictionary<object, object>
        {
            { AuditAzureFunctionMiddleware.AuditEventKey, evt }
        };
        var context = CreateContextWithItems(items);

        // Act
        var scope = context.GetAuditScope();

        // Assert
        Assert.That(scope, Is.Null);
    }

    [Test]
    public void GetTriggerAttribute_DelegatesToMiddleware_ReturnsNull_WhenMiddlewareHasNoData()
    {
        var context = CreateContextWithItems(new Dictionary<object, object>());

        var attr = context.GetTriggerAttribute();

        Assert.That(attr, Is.Null);
    }

    [Test]
    public void GetTriggerData_DelegatesToMiddleware_ReturnsNull_WhenMiddlewareHasNoData()
    {
        var context = CreateContextWithItems(new Dictionary<object, object>());

        var trigger = context.GetTriggerData();

        Assert.That(trigger, Is.Null);
    }

    [Test]
    public void GetAzureFunctionDefinition_DelegatesToMiddleware_ReturnsNull_WhenMiddlewareHasNoData()
    {
        var context = CreateContextWithItems(new Dictionary<object, object>());

        var def = context.GetAzureFunctionDefinition();

        Assert.That(def, Is.Not.Null);
        Assert.That(def.Id, Is.EqualTo("DEF-1"));
    }
}