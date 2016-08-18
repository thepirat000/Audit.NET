using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Audit.Core
{
    /// <summary>
    /// Makes a code block auditable.
    /// </summary>
    /// <typeparam name="T">The type of the object to audit</typeparam>
    public class AuditScope<T> : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Creates an audit scope from a reference value, and a reference Id.
        /// </summary>
        /// <param name="reference">The reference object getter.</param>
        /// <param name="referenceId">The reference id.</param>
        public AuditScope(Func<T> reference, string referenceId)
            : this(typeof(T).Name, () => reference(), referenceId, 2)
        {
        }

        /// <summary>
        /// Creates an audit scope from a reference value, an event type and a reference Id.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="reference">The reference object getter.</param>
        /// <param name="referenceId">The reference id.</param>
        public AuditScope(string eventType, Func<T> reference, string referenceId)
            : this(eventType, () => reference(), referenceId, 2)
        {
        }

        protected internal AuditScope(string eventType, Func<T> reference, string referenceId,
            int callingMethodStackIndex = 1)
            : this(eventType, reference, () => referenceId, ++callingMethodStackIndex)
        {
        }

        protected internal AuditScope(string eventType, Func<T> reference, Func<string> referenceIdGetter,
            int callingMethodStackIndex = 1)
        {
            TestConnectionDataProvider();
            _referenceIdGetter = referenceIdGetter;
            _newValueGetter = () => reference();
            var callingMethod = new StackFrame(callingMethodStackIndex).GetMethod();
            _event = new AuditEvent()
            {
                Environment = new AuditEventEnvironment()
                {
                    UserName = Environment.UserName,
                    MachineName = Environment.MachineName,
                    DomainName = Environment.UserDomainName,
                    CallingMethodName = (callingMethod.DeclaringType != null ? callingMethod.DeclaringType.FullName + "." : "") + callingMethod.Name + "()",
                    Culture = System.Globalization.CultureInfo.CurrentCulture.ToString()
                },
                StartDate = DateTime.Now,
                EventType = eventType,
                Target = new AuditTarget(typeof(T).Name)
                {
                    SerializedOld = _dataProvider.Serialize(reference.Invoke())
                },
                Comments = new List<string>(),
                CustomFields = new Dictionary<string, object>()
            };
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Indicates the change type (i.e. CustomerOrder Update)
        /// </summary>
        public string EventType
        {
            get { return _event.EventType; }
            set { _event.EventType = value; }
        }

        /// <summary>
        /// Indicates the reference Identifier for the change (i.e. The CustomerOrder Id)
        /// </summary>
        public string ReferenceId
        {
            get { return _event.ReferenceId; }
            set { _event.ReferenceId = value; }
        }
        #endregion

        #region Private fields
        private readonly AuditEvent _event;
        private bool _disposed;
        private bool _saved;
        private readonly IAuditDataProvider _dataProvider = AuditConfiguration.DataProvider;
        private readonly Func<T> _newValueGetter;
        private readonly Func<string> _referenceIdGetter;
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
            if (this._newValueGetter != null)
            {
                _event.Target.SerializerNew = _dataProvider.Serialize(this._newValueGetter.Invoke());
            }
            if (this.ReferenceId == null)
            {
                this.ReferenceId = _referenceIdGetter == null ? null : _referenceIdGetter.Invoke();
            }
            if (_event.Comments.Count == 0)
            {
                _event.Comments = null;
            }
            _event.EndDate = DateTime.Now;
            _dataProvider.WriteEvent(_event);
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

        private void TestConnectionDataProvider()
        {
            if (!_dataProvider.TestConnection())
            {
                throw new Exception(string.Format("{0}: Can't connect to Audit Database.", _dataProvider.GetType().Name));
            }
        }

        #endregion
    }
}
