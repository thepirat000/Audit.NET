using Audit.AzureFunctions.ConfigurationApi;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;

namespace Audit.AzureFunctions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the audit middleware to the Azure Functions worker pipeline when the specified condition is met.
    /// </summary>
    /// <param name="builder">The application builder used to configure the Azure Functions worker pipeline.</param>
    /// <param name="when">A predicate that determines whether the audit middleware should be applied for a given function context. The
    /// middleware is added only when this function returns <see langword="true"/>.</param>
    /// <param name="options">The options used to configure the audit middleware. If not null, these options are registered as a singleton service.</param>
    public static void UseAuditMiddlewareWhen(this IFunctionsWorkerApplicationBuilder builder, Func<FunctionContext, bool> when, AuditAzureFunctionOptions options)
    {
        if (options != null)
        {
            builder.Services.AddSingleton(options);
        }

        builder.UseWhen<AuditAzureFunctionMiddleware>(when);
    }

    /// <summary>
    /// Adds the audit middleware to the Azure Functions worker pipeline when the specified condition is met, and configures its behavior.
    /// </summary>
    /// <remarks>Use this method to conditionally enable auditing for Azure Functions based on runtime context. The middleware will only be invoked when the provided predicate returns <see langword="true"/> for the current function execution.</remarks>
    /// <param name="builder">The application builder used to configure the Azure Functions worker pipeline.</param>
    /// <param name="when">A predicate that determines whether the audit middleware should be applied for a given function execution context.</param>
    /// <param name="configure">An action to configure the audit middleware options. If null, the middleware is added with default configuration.</param>
    public static void UseAuditMiddlewareWhen(this IFunctionsWorkerApplicationBuilder builder, Func<FunctionContext, bool> when, Action<IAuditAzureFunctionConfigurator> configure)
    {
        if (configure != null)
        {
            var options = new AuditAzureFunctionOptions(configure);

            builder.Services.AddSingleton(options);
        }

        builder.UseWhen<AuditAzureFunctionMiddleware>(when);
    }

    /// <summary>
    /// Adds audit middleware to the Azure Functions worker pipeline, enabling request auditing based on the specified options.
    /// </summary>
    /// <remarks>Call this method during application startup to enable auditing for all incoming requests. The provided options are registered as a singleton and used by the audit middleware. If no options are supplied, auditing will use default configuration.</remarks>
    /// <param name="builder">The application builder used to configure the Azure Functions worker pipeline.</param>
    /// <param name="options">The options that configure audit behavior for Azure Functions. If null, default audit settings are applied.</param>
    public static void UseAuditMiddleware(this IFunctionsWorkerApplicationBuilder builder, AuditAzureFunctionOptions options)
    {
        if (options != null)
        {
            builder.Services.AddSingleton(options);
        }

        builder.UseMiddleware<AuditAzureFunctionMiddleware>();
    }

    /// <summary>
    /// Adds audit logging middleware to the Azure Functions worker pipeline, allowing requests to be audited according to the specified configuration.
    /// </summary>
    /// <remarks>This method should be called during application startup to enable auditing for all incoming function requests. The middleware uses the provided configuration to determine how audit events are captured and stored.</remarks>
    /// <param name="builder">The application builder used to configure the Azure Functions worker pipeline.</param>
    /// <param name="configure">A delegate that configures audit options for the middleware. If null, default audit configuration is applied.</param>
    public static void UseAuditMiddleware(this IFunctionsWorkerApplicationBuilder builder, Action<IAuditAzureFunctionConfigurator> configure)
    {
        if (configure != null)
        {
            var options = new AuditAzureFunctionOptions(configure);

            builder.Services.AddSingleton(options);
        }

        builder.UseMiddleware<AuditAzureFunctionMiddleware>();
    }
}