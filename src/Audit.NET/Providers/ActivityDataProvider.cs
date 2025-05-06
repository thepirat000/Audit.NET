using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Audit.Core.ConfigurationApi;
#pragma warning disable CS3002 // Activity not CLS-compliant
#pragma warning disable CS3003 // Activity not CLS-compliant

namespace Audit.Core.Providers;

/// <summary>
/// An <see cref="AuditDataProvider"/> implementation that records audit events as OpenTelemetry-compatible <see cref="System.Diagnostics.Activity"/> spans.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ActivityDataProvider</c> leverages <see cref="System.Diagnostics.ActivitySource"/> to start and stop an <see cref="Activity"/> for each <see cref="AuditEvent"/>. It automatically manages the Activity’s lifecycle according to the configured event creation policy (e.g. insert-on-start/replace-on-end), and supports both one-shot and long-running events.
/// </para>
/// <para>
/// By default, the provider can emit a set of standard tags—event type, start time, end time, duration (in ms), user, machine, and any custom fields—using configurable tag keys. You can enable these default tags via <see cref="IncludeDefaultTags"/>, override their key names, or supply entirely custom tags through <see cref="AdditionalTags"/>. For advanced scenarios, the <see cref="OnActivityCreated"/> callback lets you enrich or modify the Activity after all tags have been applied.
/// </para>
/// <para>
/// Core configurable settings include:
/// <list type="bullet">
///   <item><see cref="SourceName"/> / <see cref="SourceVersion"/>: identifies the ActivitySource.</item>
///   <item><see cref="ActivityName"/>: determines the Activity’s name (defaults to the AuditEvent type).</item>
///   <item><see cref="ActivityKind"/>: sets the span kind (defaults to <see cref="System.Diagnostics.ActivityKind.Internal"/>).</item>
///   <item><see cref="IncludeDefaultTags"/>: toggles inclusion of Audit.NET’s standard tags.</item>
///   <item><see cref="AdditionalTags"/>: adds any additional user-defined tags.</item>
///   <item><see cref="OnActivityCreated"/>: a hook for arbitrary Activity enrichment logic.</item>
/// </list>
/// </para>
/// <para>
/// This provider is ideal for integrating Audit.NET with distributed tracing systems via OpenTelemetry, enabling you to correlate audit trails with application traces and visualize them in your observability backend.
/// </para>
/// <para>
/// This provider implements ReplaceEvent/ReplaceEventAsync and can be used with EventCreationPolicy.InsertOnStartReplaceOnEnd, in which case the Activity will be kept until the event is replaced.
/// </para>
/// </remarks>
public class ActivityDataProvider : AuditDataProvider
{
    /// <summary>
    /// The default tag key to use for the event type. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagEventType { get; set; } = "audit.event_type";
    /// <summary>
    /// The default tag key to use for the start time. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagStartTime { get; set; } = "audit.start_time";
    /// <summary>
    /// The default tag key to use for the end time. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagEndTime { get; set; } = "audit.end_time";
    /// <summary>
    /// The default tag key to use for the duration in milliseconds. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagDurationMs { get; set; } = "audit.duration_ms";
    /// <summary>
    /// The default tag key to use for the username. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagUser { get; set; } = "audit.user";
    /// <summary>
    /// The default tag key to use for the machine name. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagMachine { get; set; } = "audit.machine";
    /// <summary>
    /// The default tag key format to use for the custom fields. This is used to set the tag on the Activity when it is created and SkipDefaultTags is set to false.
    /// </summary>
    public static string DefaultTagCustomFieldFormat { get; set; } = "audit.custom.{0}";
    
    private static readonly ConcurrentDictionary<(string Name, string Version), ActivitySource> ActivitySources = new();

    private readonly ConcurrentDictionary<string, Activity> _activeSpans = new();

    /// <summary>
    /// The name of the ActivitySource object to use for the given AuditEvent.
    /// Defaults to "Audit.Core.Providers.ActivityDataProvider".
    /// </summary>
    public Setting<string> SourceName { get; set; } = typeof(ActivityDataProvider).FullName!;

    /// <summary>
    /// The version of the component publishing the tracing info for the given AuditEvent.
    /// Defaults to the version of the Audit.NET assembly being used.
    /// </summary>
    public Setting<string> SourceVersion { get; set; } = typeof(ActivityDataProvider).Assembly.GetName()!.Version!.ToString();

    /// <summary>
    /// The operation name of the Activity. This is used to set the name of the Activity when it is created.
    /// Defaults to the AuditEvent type name that is being audited.
    /// </summary>
    public Setting<string> ActivityName { get; set; } = new(ev => ev.GetType().Name);

    /// <summary>
    /// The ActivityKind to use for the Activity. This is used to set the kind of the Activity when it is created.
    /// Defaults to ActivityKind.Internal.
    /// </summary>
    public Setting<ActivityKind> ActivityKind { get; set; } = System.Diagnostics.ActivityKind.Internal;

    /// <summary>
    /// Indicates whether to include the Audit.NET's default tags in the Activity. Default to false meaning the default tags will not be set.
    /// <para>
    /// The default tags are:
    /// <list type="bullet">
    ///   <item>audit.event_type: The type of the event (AuditEvent.EventType)</item>
    ///   <item>audit.start_time: The start time of the event (AuditEvent.StartDate)</item>
    ///   <item>audit.end_time: The end time of the event (AuditEvent.EndDate)</item>
    ///   <item>audit.duration_ms: The duration of the event in milliseconds (AuditEvent.Duration)</item>
    ///   <item>audit.user: The username from the environment (AuditEvent.Environment.UserName)</item>
    ///   <item>audit.machine: The machine name from the environment (AuditEvent.Environment.MachineName)</item>
    ///   <item>audit.custom.{key}: The custom fields of the event (AuditEvent.CustomFields) </item>
    /// </list>
    /// </para>
    /// </summary>
    public Setting<bool> IncludeDefaultTags { get; set; } = false;

    /// <summary>
    /// Indicates whether to use the internal AuditScope activity as the activity for the given AuditEvent.
    /// <para>
    /// When this is set to true:
    /// <list type="bullet">
    ///   <item>Make sure the `StartActivityTrace` option is set to true in the `AuditScopeOptions` or in the global configuration `Audit.Core.Configuration.StartActivityTrace`</item>
    ///   <item>This data provider will enrich the activity created by the AuditScope and will not create a new one</item>
    ///   <item>The SourceName, SourceVersion and ActivityKind settings will be ignored since the AuditScope will create the activity with its own ActivitySource</item>
    /// </list>
    /// </para>
    /// </summary>
    public Setting<bool> TryUseAuditScopeActivity { get; set; } = false;

    /// <summary>
    /// The tags to set on the Activity. This is used to set additional tags on the Activity when it is created.
    /// </summary>
    public Setting<Dictionary<string, object>> AdditionalTags { get; set; }

    /// <summary>
    /// Delegate invoked to enrich an <see cref="Activity"/> with additional information after default and extra tags have been applied.
    /// This allows for setting custom tags or performing other modifications to the Activity.
    /// </summary>
    public Action<Activity, AuditEvent> OnActivityCreated { get; set; }

    /// <summary>
    /// Default constructor for <see cref="ActivityDataProvider"/>.
    /// </summary>
    public ActivityDataProvider()
    {
    }

    /// <summary>
    /// Constructs an <see cref="ActivityDataProvider"/> with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration fluent API.</param>
    public ActivityDataProvider(Action<IActivityProviderConfigurator> config)
    {
        var configurator = new ActivityProviderConfigurator();
        config.Invoke(configurator);

        SourceName = configurator._sourceName;
        SourceVersion = configurator._sourceVersion;
        ActivityName = configurator._activityName;
        ActivityKind = configurator._activityKind;
        IncludeDefaultTags = configurator._includeDefaultTags;
        AdditionalTags = configurator._additionalTags;
        OnActivityCreated = configurator._onActivityCreated;
    }

    /// <inheritdoc />
    public override object InsertEvent(AuditEvent auditEvent)
    {
        var eventId = Guid.NewGuid().ToString();

        var activity = GetOrCreateAuditActivity(auditEvent, out var reusingActivity);

        if (activity == null)
        {
            // No listeners or sampling
            return eventId;
        }

        UpdateActivity(activity, auditEvent);

        if (reusingActivity)
        {
            return eventId;
        }

        if (auditEvent.EndDate == null && auditEvent.GetScope()?.EventCreationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd)
        {
            // AuditEvent not ended yet, keep the activity
            _activeSpans[eventId] = activity!;
        }
        else
        {
            activity.Stop();
        }

        return eventId;
    }

    /// <inheritdoc />
    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        Activity activity = null;

        // Check if we are using the AuditScope activity
        if (TryUseAuditScopeActivity.GetValue(auditEvent))
        {
            activity = auditEvent.GetScope()?.GetActivity();
            if (activity != null)
            {
                UpdateActivity(activity, auditEvent);
                return;
            }
        }

        // Check if we have an active span
        if (!_activeSpans.TryRemove(eventId.ToString()!, out activity))
        {
            return;
        }

        UpdateActivity(activity, auditEvent);

        activity.Stop();
    }

    /// <summary>
    /// Creates and starts an activity for the given AuditEvent.
    /// </summary>
    private Activity GetOrCreateAuditActivity(AuditEvent auditEvent, out bool reusingActivity)
    {
        if (TryUseAuditScopeActivity.GetValue(auditEvent))
        {
            var activity = auditEvent.GetScope()?.GetActivity();

            if (activity != null)
            {
                reusingActivity = true;
                return activity;
            }
        }

        reusingActivity = false;
        return CreateAuditActivity(auditEvent);
    }

    protected virtual Activity CreateAuditActivity(AuditEvent auditEvent)
    {
        var source = GetActivitySource(auditEvent);

        var name = ActivityName.GetValue(auditEvent);
        var kind = ActivityKind.GetValue(auditEvent);

        return source.StartActivity(name, kind);
    }

    /// <summary>
    /// Gets or creates the Activity Source to use for the given AuditEvent.
    /// </summary>
    protected virtual ActivitySource GetActivitySource(AuditEvent auditEvent)
    {
        return ActivitySources.GetOrAdd((SourceName.GetValue(auditEvent), SourceVersion.GetValue(auditEvent)), key => new ActivitySource(key.Name, key.Version));
    }

    private void UpdateActivity(Activity activity, AuditEvent auditEvent)
    {
        SetActivityDefaultTags(activity, auditEvent);

        SetActivityExtraTags(activity, auditEvent);

        SetActivityNameStartAndEndTime(activity, auditEvent);
        
        CallActivityAction(activity, auditEvent);
    }

    private void SetActivityNameStartAndEndTime(Activity activity, AuditEvent auditEvent)
    {
        var name = ActivityName.GetValue(auditEvent);
        if (name != null)
        {
            activity.DisplayName = name;
        }

        activity.SetStartTime(auditEvent.StartDate);

        if (auditEvent.EndDate != null)
        {
            activity.SetEndTime(auditEvent.EndDate.Value);
        }
    }

    private void SetActivityDefaultTags(Activity activity, AuditEvent auditEvent)
    {
        if (!activity.IsAllDataRequested || !IncludeDefaultTags.GetValue(auditEvent))
        {
            return;
        }

        activity.SetTag(DefaultTagEventType, auditEvent.EventType);
        activity.SetTag(DefaultTagStartTime, auditEvent.StartDate.ToString("o"));

        if (auditEvent.EndDate != null)
        {
            activity.SetTag(DefaultTagEndTime, auditEvent.EndDate.Value.ToString("o"));
            activity.SetTag(DefaultTagDurationMs, auditEvent.Duration);
        }

        var env = auditEvent.Environment;

        if (env != null)
        {
            activity.SetTag(DefaultTagUser, env.UserName);
            activity.SetTag(DefaultTagMachine, env.MachineName);
        }

        foreach (var kv in auditEvent.CustomFields ?? [])
        {
            activity.SetTag(string.Format(DefaultTagCustomFieldFormat, kv.Key), kv.Value);
        }
    }

    private void SetActivityExtraTags(Activity activity, AuditEvent auditEvent)
    {
        if (!activity.IsAllDataRequested)
        {
            return;
        }

        var tags = AdditionalTags.GetValue(auditEvent) ?? [];

        foreach (var kv in tags)
        {
            activity.SetTag(kv.Key, kv.Value);
        }
    }

    private void CallActivityAction(Activity activity, AuditEvent auditEvent)
    {
        OnActivityCreated?.Invoke(activity, auditEvent);
    }
}