using System;
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
        /// Gets or sets the event creation policy to use.
        /// </summary>
        public EventCreationPolicy? CreationPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this scope should be immediately saved after creation
        /// </summary>
        public bool IsCreateAndSave { get; set; }

        /// <summary>
        /// Gets or sets the initial audit event to use, or NULL to create a new instance of AuditEvent
        /// </summary>
        public AuditEvent AuditEvent { get; set; }

        /// <summary>
        /// Gets or sets the value used to indicate how many frames in the stack should be skipped to determine the calling method
        /// </summary>
        public int SkipExtraFrames { get; set; }

        /// <summary>
        /// Gets or sets a specific calling method to store on the event. NULL to use the calling stack to determine the calling method.
        /// </summary>
        public MethodBase CallingMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audit event's environment should include the full stack trace.
        /// </summary>
        public bool IncludeStackTrace { get; set; }
#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets or sets a value indicating whether the audit event should include the Distributed Tracing Activity data.
        /// </summary>
        public bool IncludeActivityTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audit scope should create and start a new Distributed Tracing Activity.
        /// </summary>
        public bool StartActivityTrace { get; set; }
#endif
        /// <summary>
        /// Creates an instance of options for an audit scope creation.
        /// </summary>
        /// <param name="eventType">A string representing the type of the event.</param>
        /// <param name="targetGetter">The target object getter.</param>
        /// <param name="extraFields">An anonymous object that contains additional fields to be merged into the audit event.</param>
        /// <param name="creationPolicy">The event creation policy to use. NULL to use the configured default creation policy.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        /// <param name="isCreateAndSave">To indicate if the scope should be immediately saved after creation.</param>
        /// <param name="auditEvent">The initialized audit event to use, or NULL to create a new instance of AuditEvent.</param>
        /// <param name="skipExtraFrames">Used to indicate how many frames in the stack should be skipped to determine the calling method.</param>
        /// <param name="includeStackTrace">Used to indicate whether the audit event's environment should include the full stack trace.</param>
        /// <param name="includeActivityTrace">Used to indicate whether the audit event should include the activity trace.</param>
        /// <param name="startActivityTrace">Used to indicate whether the audit scope should create and start a new activity.</param>
        public AuditScopeOptions(
            string eventType = null,
            Func<object> targetGetter = null,
            object extraFields = null,
            AuditDataProvider dataProvider = null,
            EventCreationPolicy? creationPolicy = null,
            bool isCreateAndSave = false,
            AuditEvent auditEvent = null,
            int skipExtraFrames = 0,
            bool? includeStackTrace = null,
            bool? includeActivityTrace = null,
            bool? startActivityTrace = null)
        {
            EventType = eventType;
            TargetGetter = targetGetter;
            ExtraFields = extraFields;
            CreationPolicy = creationPolicy ?? Configuration.CreationPolicy;
            DataProvider = dataProvider ?? Configuration.DataProvider;
            IsCreateAndSave = isCreateAndSave;
            AuditEvent = auditEvent;
            SkipExtraFrames = skipExtraFrames;
            CallingMethod = null;
            IncludeStackTrace = includeStackTrace ?? Configuration.IncludeStackTrace;
#if NET6_0_OR_GREATER
            IncludeActivityTrace = includeActivityTrace ?? Configuration.IncludeActivityTrace;
            StartActivityTrace = startActivityTrace ?? Configuration.StartActivityTrace;
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditScopeOptions"/> class.
        /// </summary>
        public AuditScopeOptions()
            : this(null, null, null, null, null)
        {
        }

        public AuditScopeOptions(Action<IAuditScopeOptionsConfigurator> config)
            : this(null, null, null, null, null)
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
#if NET6_0_OR_GREATER
                IncludeActivityTrace = scopeConfig._options.IncludeActivityTrace;
                StartActivityTrace = scopeConfig._options.StartActivityTrace;
#endif
            }

        }
    }
}
