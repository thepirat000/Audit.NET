using System;
using System.Reflection;

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
        /// Gets or Sets the data provider builder.
        /// </summary>
        public Func<AuditDataProvider> DataProviderFactory { get; set; }

        /// <summary>
        /// Gets or sets the data provider to use.
        /// </summary>
        public AuditDataProvider DataProvider { get { return DataProviderFactory?.Invoke(); } set { DataProviderFactory = () => value; } }

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

        /// <summary>
        /// Gets or sets a value indicating whether the audit event should include the Distributed Tracing Activity data (ID, .
        /// </summary>
        public bool IncludeActivityTrace { get; set; }

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
            bool? includeActivityTrace = null)
        {
            EventType = eventType;
            TargetGetter = targetGetter;
            ExtraFields = extraFields;
            CreationPolicy = creationPolicy ?? Configuration.CreationPolicy;
            DataProviderFactory = dataProvider != null ? () => dataProvider : Configuration.DataProviderFactory;
            IsCreateAndSave = isCreateAndSave;
            AuditEvent = auditEvent;
            SkipExtraFrames = skipExtraFrames;
            CallingMethod = null;
            IncludeStackTrace = includeStackTrace ?? Configuration.IncludeStackTrace;
            IncludeActivityTrace = includeActivityTrace ?? Configuration.IncludeActivityTrace;
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
                DataProviderFactory = scopeConfig._options.DataProviderFactory;
                IsCreateAndSave = scopeConfig._options.IsCreateAndSave;
                AuditEvent = scopeConfig._options.AuditEvent;
                SkipExtraFrames = scopeConfig._options.SkipExtraFrames;
                CallingMethod = scopeConfig._options.CallingMethod;
                IncludeStackTrace = scopeConfig._options.IncludeStackTrace;
                IncludeActivityTrace = scopeConfig._options.IncludeActivityTrace;
            }

        }
    }
}
