using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.AzureFunctions;
using Audit.AzureFunctions.ConfigurationApi;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Moq;

using NUnit.Framework;

namespace Audit.AzureFunctions.UnitTest;

public class ApplicationBuilderExtensionsTests
{
    // A minimal test double implementing IFunctionsWorkerApplicationBuilder to capture middleware registrations.
    private class TestFunctionsWorkerApplicationBuilder : IFunctionsWorkerApplicationBuilder
    {
        public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
        {
            return this;
        }

        public IServiceCollection Services { get; } = new ServiceCollection();

        public IFunctionsWorkerApplicationBuilder UseMiddleware<TMiddleware>() where TMiddleware : class
        {
            // Record that a middleware was added
            Services.AddSingleton(typeof(TMiddleware), new object());
            return this;
        }

        public IFunctionsWorkerApplicationBuilder UseWhen<TMiddleware>(Func<FunctionContext, bool> predicate) where TMiddleware : class
        {
            // Record that a conditional middleware was added and store the predicate so tests can evaluate it
            Services.AddSingleton(typeof(TMiddleware), new object());
            Services.AddSingleton(predicate);
            return this;
        }
    }

    private static FunctionContext CreateDummyFunctionContext()
    {
        // FunctionContext is abstract; use Moq to create a minimal instance
        var ctx = new Mock<FunctionContext>(MockBehavior.Strict).Object;
        return ctx;
    }

    [Test]
    public void UseAuditMiddlewareWhen_WithOptions_AddsSingletonAndConditionalMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();
        var options = new AuditAzureFunctionOptions(cfg => cfg.EventType("X"));

        Func<FunctionContext, bool> predicate = _ => true;

        // Act
        builder.UseAuditMiddlewareWhen(predicate, options);

        // Assert: options registered as singleton
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Not.Null);
        Assert.That(optionsRegistration.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);

        // Assert: options
        var optionsFromServices = builder.Services
            .Where(d => d.ServiceType == typeof(AuditAzureFunctionOptions))
            .Select(d => d.ImplementationInstance)
            .OfType<AuditAzureFunctionOptions>()
            .FirstOrDefault();
        Assert.That(optionsFromServices, Is.Not.Null);
        Assert.That(optionsFromServices.EventType.Invoke(null), Is.EqualTo("X"));
    }

    [Test]
    public void UseAuditMiddlewareWhen_WithNullOptions_DoesNotRegisterOptions_StillAddsConditionalMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();
        Func<FunctionContext, bool> predicate = _ => false;

        // Act
        builder.UseAuditMiddlewareWhen(predicate, (AuditAzureFunctionOptions)null);

        // Assert: options not registered
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Null);

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }

    [Test]
    public void UseAuditMiddlewareWhen_WithConfigurator_AddsSingletonAndConditionalMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();
        Func<FunctionContext, bool> predicate = _ => true;

        // Act
        builder.UseAuditMiddlewareWhen(predicate, cfg =>
        {
            cfg.EventType("FUNC {name} ({trigger})").IncludeFunctionDefinition().IncludeTriggerInfo();
        });
            
        // Assert: options registered as singleton
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Not.Null);
        Assert.That(optionsRegistration.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }

    [Test]
    public void UseAuditMiddlewareWhen_WithNullConfigurator_DoesNotRegisterOptions_StillAddsConditionalMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();
        Func<FunctionContext, bool> predicate = _ => true;

        // Act
        builder.UseAuditMiddlewareWhen(predicate, (Action<IAuditAzureFunctionConfigurator>)null);

        // Assert: options not registered
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Null);

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }

    [Test]
    public void UseAuditMiddleware_WithOptions_AddsSingletonAndMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();
        var options = new AuditAzureFunctionOptions(cfg => cfg.EventType("ABC"));

        // Act
        builder.UseAuditMiddleware(options);

        // Assert: options registered
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Not.Null);
        Assert.That(optionsRegistration.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }

    [Test]
    public void UseAuditMiddleware_WithNullOptions_DoesNotRegisterOptions_StillAddsMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();

        // Act
        builder.UseAuditMiddleware((AuditAzureFunctionOptions)null);

        // Assert: options not registered
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Null);

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }

    [Test]
    public void UseAuditMiddleware_WithConfigurator_AddsSingletonAndMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();

        // Act
        builder.UseAuditMiddleware(cfg =>
        {
            cfg.EventType("FUNC {name}").IncludeTriggerInfo().IncludeFunctionDefinition();
        });

        // Assert: options registered
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Not.Null);
        Assert.That(optionsRegistration.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }

    [Test]
    public void UseAuditMiddleware_WithNullConfigurator_DoesNotRegisterOptions_StillAddsMiddleware()
    {
        // Arrange
        var builder = new TestFunctionsWorkerApplicationBuilder();

        // Act
        builder.UseAuditMiddleware((Action<IAuditAzureFunctionConfigurator>)null);

        // Assert: options not registered
        var optionsRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionOptions));
        Assert.That(optionsRegistration, Is.Null);

        // Assert: middleware registered
        var mwRegistration = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(AuditAzureFunctionMiddleware));
        Assert.That(mwRegistration, Is.Not.Null);
    }
}