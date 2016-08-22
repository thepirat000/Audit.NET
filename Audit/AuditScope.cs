using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        /// Creates an audit scope from a reference value, and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        public AuditScope(string eventType)
            : this(eventType, null, null, 2)
        {
        }

        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        public AuditScope(string eventType, Func<object> target)
            : this(eventType, () => target(), null, 2)
        {
        }

        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="target">The reference object getter.</param>
        /// <param name="extraFields">An anonymous object that can contain additional fields will be merged into the audit event.</param>
        public AuditScope(string eventType, Func<object> target, object extraFields)
            : this(eventType, () => target(), extraFields, 2)
        {
        }

        protected internal AuditScope(string eventType, Func<object> target, object extraFields = null, int callingMethodStackIndex = 1)
        {
            _targetGetter = target;
            var callingMethod = new StackFrame(callingMethodStackIndex).GetMethod();
            _event = new AuditEvent()
            {
                Environment = new AuditEventEnvironment()
                {
                    UserName = Environment.UserName,
                    MachineName = Environment.MachineName,
                    DomainName = Environment.UserDomainName,
                    CallingMethodName = (callingMethod.DeclaringType != null ? callingMethod.DeclaringType.FullName + "." : "") + callingMethod.Name + "()",
                    AssemblyName = callingMethod.DeclaringType?.Assembly.FullName,
                    Culture = System.Globalization.CultureInfo.CurrentCulture.ToString()
                },
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
            _dataProvider.Init(_event);
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
        #endregion

        #region Private fields
        private readonly AuditEvent _event;
        private bool _disposed;
        private bool _saved;
        private readonly AuditDataProvider _dataProvider = AuditConfiguration.DataProvider;
        private readonly Func<object> _targetGetter;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        public void Comment(string text)
        {
            _event.Comments.Add(text); 
        }

        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        public void Comment(string format, params object[] args)
        {
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
            Save();
        }

        /// <summary>
        /// Discards this audit scope, so the event will not be written.
        /// </summary>
        public void Discard()
        {
            // Mark as saved to ignore the saving
            _saved = true;
        }

        /// <summary>
        /// Saves the event.
        /// </summary>
        public void Save()
        {
            if (_saved)
            {
                return;
            }
            var exception = GetCurrentException();
            _event.Environment.Exception = exception != null ? string.Format("{0}: {1}", exception.GetType().Name, exception.Message) : null;
            if (_targetGetter != null)
            {
                _event.Target.SerializedNew = _dataProvider.Serialize(_targetGetter.Invoke());
            }
            if (_event.Comments.Count == 0)
            {
                _event.Comments = null;
            }
            _event.EndDate = DateTime.Now;
            _dataProvider.End(_event);
            _saved = true;
        }
        #endregion

        #region Private Methods
        private static Exception GetCurrentException()
        {
            if (Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0)
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
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(extraFields))
            {
                SetCustomField(prop.Name, prop.GetValue(extraFields));
            }
        }
        #endregion
    }
}
