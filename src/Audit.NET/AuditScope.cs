using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Audit.Core
{
    /// <summary>
    /// Makes a code block auditable.
    /// </summary>
    /// <typeparam name="T">The type of the object to audit</typeparam>
    public partial class AuditScope : IDisposable
    {
        #region Constructors
        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The target object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields will be merged into the audit event.</param>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        /// <param name="dataProvider">The data provider to use. NULL to use the configured default data provider.</param>
        /// <param name="isCreateAndSave">To indicate if the scope should be immediately saved after creation.</param>
        protected internal AuditScope(string eventType, Func<object> target, object extraFields = null, 
            AuditDataProvider dataProvider = null, 
            EventCreationPolicy? creationPolicy = null,
            bool isCreateAndSave = false)
        {
            _creationPolicy = creationPolicy ?? Configuration.CreationPolicy;
            _dataProvider = dataProvider ?? Configuration.DataProvider;
            _targetGetter = target;
            var environment = new AuditEventEnvironment()
            {
                Culture = System.Globalization.CultureInfo.CurrentCulture.ToString(),
            };
#if NET45
            //This will be possible in future NETStandard: 
            //See: https://github.com/dotnet/corefx/issues/1797, https://github.com/dotnet/corefx/issues/1784
            var callingMethod = new StackFrame(2).GetMethod();
            environment.UserName = Environment.UserName;
            environment.MachineName = Environment.MachineName;
            environment.DomainName = Environment.UserDomainName;
            environment.CallingMethodName = (callingMethod.DeclaringType != null
                ? callingMethod.DeclaringType.FullName + "."
                : "") + callingMethod.Name + "()";
            environment.AssemblyName = callingMethod.DeclaringType?.Assembly.FullName;
#endif
            _event = new AuditEvent()
            {
                Environment = environment,
                StartDate = DateTime.Now,
                EventType = eventType,
                Comments = new List<string>(),
                CustomFields = new Dictionary<string, object>()
            };
            if (target != null)
            {
                var targetValue = target.Invoke();
                _event.Target = new AuditTarget
                {
                    SerializedOld = _dataProvider.Serialize(targetValue),
                    Type = targetValue?.GetType().Name ?? "Object"
                };
            }
            ProcessExtraFields(extraFields);
            // Execute custom on-saving actions
            Configuration.InvokeScopeCustomActions(ActionType.OnScopeCreated, this);

            // Process the event insertion (if applies)
            if (isCreateAndSave)
            {
                EndEvent();
                SaveEvent();
                _ended = true;
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd || _creationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                SaveEvent();
            }
        }
#endregion

#region Public Properties
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
        #endregion

        #region Private fields
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
        /// Add a custom field to the event.
        /// </summary>
        public void SetCustomField<TC>(string fieldName, TC value)
        {
            if (value == null)
            {
                return;
            }
            _event.CustomFields[fieldName] = _dataProvider.Serialize(value);
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
        /// Use this method when the Data Provider's CreationPolicy is set to Manual.
        /// </summary>
        public void Save()
        {
            if (_creationPolicy != EventCreationPolicy.Manual)
            {
                return;
            }
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
            
            foreach (var prop in extraFields.GetType().GetRuntimeProperties())
            {
                SetCustomField(prop.Name, prop.GetValue(extraFields));
            }
        }

        private void SaveEvent(bool forceInsert = false)
        {
            if (_ended)
            {
                return; 
            }
            Configuration.InvokeScopeCustomActions(ActionType.OnEventSaving, this);
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
