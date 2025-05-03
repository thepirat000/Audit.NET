using System;
using System.Collections.Generic;
using System.Diagnostics;
using Audit.Core.Providers;
#pragma warning disable CS3001 // Activity not CLS-compliant

namespace Audit.Core.ConfigurationApi;

public class ActivityProviderConfigurator : IActivityProviderConfigurator
{
    internal Setting<string> _sourceName = typeof(ActivityDataProvider).FullName!;
    internal Setting<string> _sourceVersion = typeof(ActivityDataProvider).Assembly.GetName()!.Version!.ToString();
    internal Setting<string> _activityName = new(ev => ev.GetType().Name);
    internal Setting<ActivityKind> _activityKind = System.Diagnostics.ActivityKind.Internal;
    internal Setting<bool> _includeDefaultTags = false;
    internal Func<AuditEvent, Dictionary<string, object>> _additionalTags;
    internal Action<Activity, AuditEvent> _onActivityCreated;

    public IActivityProviderConfigurator Source(string name)
    {
        _sourceName = name;
        _sourceVersion = (string)null;
        
        return this;
    }

    public IActivityProviderConfigurator Source(string name, string version)
    {
        _sourceName = name;
        _sourceVersion = version;

        return this;
    }

    public IActivityProviderConfigurator Source(Func<AuditEvent, string> nameBuilder)
    {
        _sourceName = nameBuilder;
        _sourceVersion = (string)null;

        return this;
    }

    public IActivityProviderConfigurator Source(Func<AuditEvent, string> nameBuilder, Func<AuditEvent, string> versionBuilder)
    {
        _sourceName = nameBuilder;
        _sourceVersion = versionBuilder;

        return this;
    }

    public IActivityProviderConfigurator ActivityName(string activityName)
    {
        _activityName = activityName;

        return this;
    }

    public IActivityProviderConfigurator ActivityName(Func<AuditEvent, string> activityNameBuilder)
    {
        _activityName = activityNameBuilder;

        return this;
    }

    public IActivityProviderConfigurator ActivityKind(ActivityKind kind)
    {
        _activityKind = kind;

        return this;
    }

    public IActivityProviderConfigurator ActivityKind(Func<AuditEvent, ActivityKind> kindBuilder)
    {
        _activityKind = kindBuilder;

        return this;
    }

    public IActivityProviderConfigurator IncludeDefaultTags(bool include = true)
    {
        _includeDefaultTags = include;

        return this;
    }

    public IActivityProviderConfigurator IncludeDefaultTags(Func<AuditEvent, bool> includeBuilder)
    {
        _includeDefaultTags = includeBuilder;

        return this;
    }

    public IActivityProviderConfigurator AdditionalTags(Func<AuditEvent, Dictionary<string, object>> additionalTagsBuilder)
    {
        _additionalTags = additionalTagsBuilder;

        return this;
    }

    public IActivityProviderConfigurator OnActivityCreated(Action<Activity, AuditEvent> onActivityCreated)
    {
        _onActivityCreated = onActivityCreated;

        return this;
    }
}