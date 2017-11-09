using Audit.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Audit.Core
{
    /// <summary>
    /// Makes a code block auditable.
    /// </summary>
    public partial class AuditScope : IDisposable
    {
        #region Constructors
        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="options">The creation options to use</param>
        /// 
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal AuditScope(AuditScopeOptions options)
        {
            _creationPolicy = options.CreationPolicy ?? Configuration.CreationPolicy;
            _dataProvider = options.DataProvider ?? Configuration.DataProvider;
            _targetGetter = options.TargetGetter;
            var environment = new AuditEventEnvironment()
            {
                Culture = System.Globalization.CultureInfo.CurrentCulture.ToString(),
            };
            MethodBase callingMethod = options.CallingMethod;
#if NET45 || NET40
            //This will be possible in future NETStandard: 
            //See: https://github.com/dotnet/corefx/issues/1797, https://github.com/dotnet/corefx/issues/1784
            environment.UserName = Environment.UserName;
            environment.MachineName = Environment.MachineName;
            environment.DomainName = Environment.UserDomainName;
            if (callingMethod == null)
            {
                callingMethod = new StackFrame(2 + options.SkipExtraFrames).GetMethod();
            }
#elif NETSTANDARD1_3
            environment.MachineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            environment.UserName = Environment.GetEnvironmentVariable("USERNAME");
#endif
            if (callingMethod != null)
            {
                environment.CallingMethodName = (callingMethod.DeclaringType != null ? callingMethod.DeclaringType.FullName + "." : "") 
                    + callingMethod.Name + "()";
#if NET40
                environment.AssemblyName = callingMethod.DeclaringType?.Assembly.FullName;
#else
                environment.AssemblyName = callingMethod.DeclaringType?.GetTypeInfo().Assembly.FullName;
#endif
            }
            _event = options.AuditEvent ?? new AuditEvent();
            _event.Environment = environment;
            _event.StartDate = DateTime.Now;
            _event.EventType = options.EventType;
            _event.CustomFields = new Dictionary<string, object>();

            if (options.TargetGetter != null)
            {
                var targetValue = options.TargetGetter.Invoke();
                _event.Target = new AuditTarget
                {
                    SerializedOld = _dataProvider.Serialize(targetValue),
                    Type = targetValue?.GetType().GetFullTypeName() ?? "Object"
                };
            }
            ProcessExtraFields(options.ExtraFields);
            _saveMode = SaveMode.InsertOnStart;
            // Execute custom on scope created actions
            Configuration.InvokeScopeCustomActions(ActionType.OnScopeCreated, this);

            // Process the event insertion (if applies)
            if (options.IsCreateAndSave)
            {
                EndEvent();
                SaveEvent();
                _ended = true;
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd || _creationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                SaveEvent();
                if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd)
                {
                    _saveMode = SaveMode.ReplaceOnEnd;
                }
                else
                {
                    _saveMode = SaveMode.InsertOnEnd;
                }
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnEnd)
            {
                _saveMode = SaveMode.InsertOnEnd;
            }
            else if (_creationPolicy == EventCreationPolicy.Manual)
            {
                _saveMode = SaveMode.Manual;
            }
        }
#endregion

#region Public Properties
        /// <summary>
        /// The current save mode. Useful on custom actions to determine the saving trigger.
        /// </summary>
        public SaveMode SaveMode
        {
            get { return _saveMode; }
        }

        /// <summary>
        /// Indicates the change type
        /// </summary>
        public string EventType
        {
            get { return _event.EventType; }
            set { _event.EventType = value; }
        }

        /// <summary>
        /// Gets the event related to this scope.
        /// </summary>
        public AuditEvent Event
        {
            get { return _event; }
        }

        /// <summary>
        /// Gets the data provider for this AuditScope instance.
        /// </summary>
        public AuditDataProvider DataProvider
        {
            get { return _dataProvider; }
        }

        /// <summary>
        /// Gets the current event ID, or NULL if not yet created.
        /// </summary>
        public object EventId
        {
            get
            {
                return _eventId;
            }
        }

        /// <summary>
        /// Gets the creation policy for this scope.
        /// </summary>
        public EventCreationPolicy EventCreationPolicy
        {
            get
            {
                return _creationPolicy;
            }
        }
        #endregion

        #region Private fields
        private SaveMode _saveMode;
        private EventCreationPolicy _creationPolicy;
        private readonly AuditEvent _event;
        private object _eventId;
        private bool _disposed;
        private bool _ended;
        private readonly AuditDataProvider _dataProvider;
        private readonly Func<object> _targetGetter;
#endregion

#region Public Methods
        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        public void Comment(string text)
        {
            Comment(text, new object[0]); 
        }

        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        public void Comment(string format, params object[] args)
        {
            if (_event.Comments == null)
            {
                _event.Comments = new List<string>();
            }
            _event.Comments.Add(string.Format(format, args));
        }

        /// <summary>
        /// Adds a custom field to the event
        /// </summary>
        /// <typeparam name="TC">The type of the value.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value object.</param>
        /// <param name="serialize">if set to <c>true</c> the field is serialized immediately.</param>
        public void SetCustomField<TC>(string fieldName, TC value, bool serialize = false)
        {
            _event.CustomFields[fieldName] = serialize ? _dataProvider.Serialize(value) : value;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            End();
        }

        /// <summary>
        /// Discards this audit scope, so the event will not be written.
        /// </summary>
        public void Discard()
        {
            // Mark as saved to ignore the saving
            _ended = true;
        }

        /// <summary>
        /// Saves the event.
        /// </summary>
        private void End()
        {
            if (_ended)
            {
                return;
            }
            EndEvent();
            // process event creation/replacement
            if (_creationPolicy == EventCreationPolicy.InsertOnEnd || _creationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                SaveEvent(true);
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd)
            {
                SaveEvent();
            }
            _ended = true;
        }

        /// <summary>
        /// Manually Saves (insert/replace) the Event.
        /// Use this method to save (insert/replace) the event when CreationPolicy is set to Manual.
        /// </summary>
        public void Save()
        {
            if (_ended)
            {
                return;
            }
            EndEvent();
            SaveEvent();
        }
#endregion

#region Private Methods

        // Update event info prior to save
        private void EndEvent()
        {
            var exception = GetCurrentException();
            _event.Environment.Exception = exception != null ? string.Format("{0}: {1}", exception.GetType().Name, exception.Message) : null;
            _event.EndDate = DateTime.Now;
            _event.Duration = Convert.ToInt32((_event.EndDate.Value - _event.StartDate).TotalMilliseconds);
            if (_targetGetter != null)
            {
                _event.Target.SerializedNew = _dataProvider.Serialize(_targetGetter.Invoke());
            }
        }

        private static Exception GetCurrentException()
        {
            if (Marshal.GetExceptionCode() != 0)
            {
                return Marshal.GetExceptionForHR(Marshal.GetExceptionCode());
            }
            return null;
        }

        private void ProcessExtraFields(object extraFields)
        {
            if (extraFields == null)
            {
                return;
            }
            var props =
#if NET40
                extraFields.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
#else
                extraFields.GetType().GetRuntimeProperties();
#endif
            foreach (var prop in props)
            {
                SetCustomField(prop.Name, prop.GetValue(extraFields, null));
            }
        }

        private void SaveEvent(bool forceInsert = false)
        {
            if (_ended)
            {
                return; 
            }
            // Execute custom on event saving actions
            Configuration.InvokeScopeCustomActions(ActionType.OnEventSaving, this);
            if (_ended)
            {
                return;
            }
            if (_eventId != null && !forceInsert)
            {
                _dataProvider.ReplaceEvent(_eventId, _event);
            }
            else
            {
                _eventId = _dataProvider.InsertEvent(_event);
            }
        }

#endregion
    }
}
