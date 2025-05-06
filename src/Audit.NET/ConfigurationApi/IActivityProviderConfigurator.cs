using System;
using System.Collections.Generic;
using System.Diagnostics;
#pragma warning disable CS3001 // Activity not CLS-compliant

namespace Audit.Core.ConfigurationApi;

/// <summary>
/// Provides a configuration for the ActivityDataProvider.
/// </summary>
public interface IActivityProviderConfigurator
{
    /// <summary>
    /// Specifies the ActivitySource name to use.
    /// </summary>
    /// <param name="name">The ActivitySource name</param>
    IActivityProviderConfigurator Source(string name);

    /// <summary>
    /// Specifies the ActivitySource name and version to use.
    /// </summary>
    /// <param name="name">The ActivitySource name</param>
    /// <param name="version">The ActivitySource version</param>
    IActivityProviderConfigurator Source(string name, string version);

    /// <summary>
    /// Specifies the function to obtain the ActivitySource name to use.
    /// </summary>
    /// <param name="nameBuilder">The ActivitySource name function</param>
    IActivityProviderConfigurator Source(Func<AuditEvent, string> nameBuilder);

    /// <summary>
    /// Specifies the function to obtain the ActivitySource name and version to use for a given AuditEvent.
    /// </summary>
    /// <param name="nameBuilder">The ActivitySource name function</param>
    /// <param name="versionBuilder">The ActivitySource version function</param>
    /// <returns></returns>
    IActivityProviderConfigurator Source(Func<AuditEvent, string> nameBuilder, Func<AuditEvent, string> versionBuilder);

    /// <summary>
    /// Specifies the Activity name to use.
    /// </summary>
    /// <param name="activityName">The Activity name</param>
    IActivityProviderConfigurator ActivityName(string activityName);

    /// <summary>
    /// Specifies the function to obtain the Activity name to use for a given AuditEvent.
    /// </summary>
    /// <param name="activityNameBuilder">The Activity name function</param>
    IActivityProviderConfigurator ActivityName(Func<AuditEvent, string> activityNameBuilder);

    /// <summary>
    /// Specifies the ActivityKind to use.
    /// </summary>
    /// <param name="kind">The ActivityKind</param>
    IActivityProviderConfigurator ActivityKind(ActivityKind kind);

    /// <summary>
    /// Specifies the function to obtain the ActivityKind to use for a given AuditEvent.
    /// </summary>
    /// <param name="kindBuilder">The ActivityKind function</param>
    IActivityProviderConfigurator ActivityKind(Func<AuditEvent, ActivityKind> kindBuilder);

    /// <summary>
    /// Indicates whether the Activity should include the Audit.NET's default tags.
    /// </summary>
    /// <param name="include">A boolean value indicating whether the default tags should be included.</param>
    IActivityProviderConfigurator IncludeDefaultTags(bool include = true);

    /// <summary>
    /// Specifies the function to determine if the Activity should include the Audit.NET's default tags.
    /// </summary>
    /// <param name="includeBuilder">The function that takes an AuditEvent and returns a boolean value indicating whether the default tags should be included.</param>
    IActivityProviderConfigurator IncludeDefaultTags(Func<AuditEvent, bool> includeBuilder);

    /// <summary>
    /// Specifies the function to obtain the additional tags to include for a given AuditEvent.
    /// </summary>
    /// <param name="additionalTagsBuilder">The function that takes an AuditEvent and returns a dictionary of additional tags.</param>
    IActivityProviderConfigurator AdditionalTags(Func<AuditEvent, Dictionary<string, object>> additionalTagsBuilder);

    /// <summary>
    /// Specifies the delegate invoked to enrich an Activity with additional information.
    /// </summary>
    /// <param name="onActivityCreated">The action that takes an Activity and an AuditEvent.</param>
    IActivityProviderConfigurator OnActivityCreated(Action<Activity, AuditEvent> onActivityCreated);

    /// <summary>
    /// Indicates whether to use the activity created by the AuditScope instead of creating a new one.
    /// <para>
    /// The AuditScope activity will be reused only when StartActivityTrace configuration is enabled (Audit.Core.Configuration.StartActivityTrace = true) and only if there are listeners configured for the source "Audit.Core.AuditScope".
    /// </para>
    /// </summary>
    /// <param name="tryUseAuditScopeActivity">A boolean value indicating whether to use the activity created by the AuditScope.</param>
    IActivityProviderConfigurator TryUseAuditScopeActivity(bool tryUseAuditScopeActivity = true);

    /// <summary>
    /// Indicates whether to use the activity created by the AuditScope instead of creating a new one.
    /// <para>
    /// The AuditScope activity will be reused only when StartActivityTrace configuration is enabled (Audit.Core.Configuration.StartActivityTrace = true) and only if there are listeners configured for the source "Audit.Core.AuditScope". 
    /// </para>
    /// </summary>
    /// <param name="tryUseAuditScopeActivityBuilder">A function that takes an AuditEvent and returns a boolean value indicating whether to use the activity created by the AuditScope.</param>
    IActivityProviderConfigurator TryUseAuditScopeActivity(Func<AuditEvent, bool> tryUseAuditScopeActivityBuilder);
}