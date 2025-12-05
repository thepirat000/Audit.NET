using Audit.Core;

using System;

namespace Audit.MediatR.ConfigurationApi;

/// <summary>
/// <para>Options for configuring MediatR auditing behavior.</para>
/// <para>You can obtain and configure an instance in two ways:</para>
/// <list type="bullet">
///   <item>
///     <description>Via the <see cref="AuditMediatRConfigurator"/> fluent API.</description>
///   </item>
///   <item>
///     <description>By directly instantiating <see cref="AuditMediatROptions"/> and supplying it to the registration helpers.</description>
///   </item>
/// </list>
/// </summary>
/// <remarks>
/// <para>Typical usage:</para>
/// <list type="bullet">
///   <item>
///     <description>Configurator: <c>var options = new AuditMediatRConfigurator().IncludeRequest().IncludeResponse().Options;</c></description>
///   </item>
///   <item>
///     <description>Direct: <c>var options = new AuditMediatROptions { /* set properties */ };</c></description>
///   </item>
/// </list>
/// </remarks>
public class AuditMediatROptions
{
    /// <summary>
    /// A filter to determine whether a MediatR call should be audited or not. Default is NULL to audit all calls.
    /// </summary>
    public Func<MediatRCallContext, bool> CallFilter { get; set; }

    /// <summary>
    /// Whether to include the request in the audit event. Default is NULL to not include the request.
    /// </summary>
    public Func<MediatRCallContext, bool> IncludeRequest { get; set; }

    /// <summary>
    /// Whether to include the response in the audit event. Default is NULL to not include the response.
    /// </summary>
    public Func<MediatRCallContext, bool> IncludeResponse { get; set; }

    /// <summary>
    /// The event creation policy to use. Default is NULL to use the globally configured creation policy.
    /// </summary>
    public EventCreationPolicy? EventCreationPolicy { get; set; }

    /// <summary>
    /// The audit data provider to use. Default is NULL to use the globally configured data provider.
    /// </summary>
    public Func<MediatRCallContext, IAuditDataProvider> DataProvider { get; set; }

    /// <summary>
    /// The Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
    /// </summary>
    public IAuditScopeFactory AuditScopeFactory { get; set; }
}