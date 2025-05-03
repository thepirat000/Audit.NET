using System;
using System.Collections.Generic;
using System.Reflection;

using Audit.Core.Providers.Wrappers;

namespace Audit.Core
{
    /// <summary>
    /// Options for AuditScope creation
    /// </summary>
    public class AuditScopeOptions
    {
        /// <summary>
        /// Gets or sets the string representing the type of the event.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the target object getter (a getter to the object to track)
        /// </summary>
        public Func<object> TargetGetter { get; set; }

        /// <summary>
        /// Gets or sets the anonymous object that contains additional fields to be merged into the audit event.
        /// </summary>
        public object ExtraFields { get; set; }

        /// <summary>
        /// Sets the data provider as a factory method that will be invoked the first time it's needed and only once. This is a shortcut to set a LazyDataProvider.
        /// </summary>
        public Func<AuditDataProvider> DataProviderFactory 
        {
            set => DataProvider = new LazyDataProvider(value);
        }
        
        /// <summary>
        /// Gets or sets the data provider to use.
        /// </summary>
        public AuditDataProvider DataProvider { get; set; }

        /// <summary>
        /// Gets or sets the event creation policy to use. When NULL, it will use the static Audit.Core.Configuration.CreationPolicy.
        /// </summary>
        public EventCreationPolicy? CreationPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this scope should be immediately saved after creation
        /// </summary>
        public bool IsCreateAndSave { get; set; }

        /// <summary>
        /// Gets or sets the initial audit event to use. When NULL, it will create a new instance of AuditEvent
        /// </summary>
        public AuditEvent AuditEvent { get; set; }

        /// <summary>
        /// Gets or sets the value used to indicate how many frames in the stack should be skipped to determine the calling method
        /// </summary>
        public int SkipExtraFrames { get; set; }

        /// <summary>
        /// Gets or sets a specific calling method to store on the event. When NULL, it will use the calling stack to determine the calling method.
        /// </summary>
        public MethodBase CallingMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audit event's environment should include the full stack trace. When NULL, it will use the static Audit.Core.Configuration.IncludeStackTrace.
        /// </summary>
        public bool? IncludeStackTrace { get; set; }

        /// <summary>
        /// Gets or sets the custom items to be included in the audit scope.
        /// </summary>
        public Dictionary<string, object> Items { get; set; } = new();

        /// <summary>
        /// Gets or sets the system clock to use. When NULL, it uses the static Audit.Core.Configuration.SystemClock.
        /// </summary>
        public ISystemClock SystemClock { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audit event should include the Distributed Tracing Activity data. When NULL, it will use the static Audit.Core.Configuration.IncludeActivityTrace.
        /// </summary>
        public bool? IncludeActivityTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audit scope should create and start a new Distributed Tracing Activity. When NULL, it will use the static Audit.Core.Configuration.StartActivityTrace.
        /// </summary>
        public bool? StartActivityTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment information should be excluded from the audit event. When NULL, it will use the static Audit.Core.Configuration.ExcludeEnvironmentInfo.
        /// </summary>
        public bool? ExcludeEnvironmentInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditScopeOptions"/> class.
        /// </summary>
        public AuditScopeOptions()
        {
        }

        public AuditScopeOptions(Action<IAuditScopeOptionsConfigurator> config)
        {
            if (config != null)
            {
                var scopeConfig = new AuditScopeOptionsConfigurator();
                config.Invoke(scopeConfig);

                EventType = scopeConfig._options.EventType;
                TargetGetter = scopeConfig._options.TargetGetter;
                ExtraFields = scopeConfig._options.ExtraFields;
                CreationPolicy = scopeConfig._options.CreationPolicy;
                DataProvider = scopeConfig._options.DataProvider;
                IsCreateAndSave = scopeConfig._options.IsCreateAndSave;
                AuditEvent = scopeConfig._options.AuditEvent;
                SkipExtraFrames = scopeConfig._options.SkipExtraFrames;
                CallingMethod = scopeConfig._options.CallingMethod;
                IncludeStackTrace = scopeConfig._options.IncludeStackTrace;
                Items = scopeConfig._options.Items;
                SystemClock = scopeConfig._options.SystemClock;
                ExcludeEnvironmentInfo = scopeConfig._options.ExcludeEnvironmentInfo;
                IncludeActivityTrace = scopeConfig._options.IncludeActivityTrace;
                StartActivityTrace = scopeConfig._options.StartActivityTrace;
            }
        }
    }
}
