using Audit.MediatR.ConfigurationApi;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using System;

namespace Audit.MediatR;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR audit behaviors (request + stream) and configures audit options via a configurator callback.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add audit behaviors to.</param>
    /// <param name="configurator"> A callback used to configure audit settings via <see cref="IAuditMediatRConfigurator"/>. </param>
    /// <param name="serviceLifetime">The lifetime to use when registering the audit behaviors. Defaults to <see cref="ServiceLifetime.Transient"/>. </param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMediatRAudit(this IServiceCollection serviceCollection, Action<IAuditMediatRConfigurator> configurator, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        serviceCollection.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(AuditMediatRBehavior<,>), serviceLifetime));

        serviceCollection.Add(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(AuditMediatRStreamBehavior<,>), serviceLifetime));

        serviceCollection.ConfigureMediatRAudit(configurator);

        return serviceCollection;
    }

    /// <summary>
    /// Registers MediatR audit behaviors (request + stream) and uses a pre-built <see cref="AuditMediatROptions"/> instance for audit settings.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add audit behaviors to.</param>
    /// <param name="options">The MediatR audit configuration to register as singleton.</param>
    /// <param name="serviceLifetime">The lifetime to use when registering the audit behaviors. Defaults to <see cref="ServiceLifetime.Transient"/>. </param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMediatRAudit(this IServiceCollection serviceCollection, AuditMediatROptions options, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        serviceCollection.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(AuditMediatRBehavior<,>), serviceLifetime));

        serviceCollection.Add(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(AuditMediatRStreamBehavior<,>), serviceLifetime));

        serviceCollection.ConfigureMediatRAudit(options);

        return serviceCollection;
    }

    /// <summary>
    /// Configures MediatR audit options via a configurator callback
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the options to.</param>
    /// <param name="auditConfigurator">A callback used to configure audit settings via <see cref="IAuditMediatRConfigurator"/>. </param>
    public static IServiceCollection ConfigureMediatRAudit(this IServiceCollection serviceCollection, Action<IAuditMediatRConfigurator> auditConfigurator)
    {
        var config = new AuditMediatRConfigurator(); 
            
        auditConfigurator.Invoke(config); 
            
        serviceCollection.AddSingleton(config.Options); 
            
        return serviceCollection;
    }

    /// <summary>
    /// Configures MediatR audit options via a pre-built <see cref="AuditMediatROptions"/> instance.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the options to.</param>
    /// <param name="options">The MediatR audit configuration to register as singleton.</param>
    public static IServiceCollection ConfigureMediatRAudit(this IServiceCollection serviceCollection, AuditMediatROptions options)
    {
        serviceCollection.AddSingleton(options);

        return serviceCollection;
    }
}