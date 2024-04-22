using Audit.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Audit.Core
{
    /// <summary>
    /// Makes a code block auditable.
    /// </summary>
    public sealed partial class AuditScope : IAuditScope
    {
        #region Constructors

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal AuditScope(AuditScopeOptions options)
        {
            _options = options;
            _creationPolicy = options.CreationPolicy ?? Configuration.CreationPolicy;
            _dataProvider = options.DataProvider ?? Configuration.DataProvider;
            _targetGetter = options.TargetGetter;

            _event = options.AuditEvent ?? new AuditEvent();
            
            _event.SetScope(this);
            
            _event.StartDate = Configuration.SystemClock.UtcNow;

            _event.Environment = GetEnvironmentInfo(options);
            
#if NET6_0_OR_GREATER
            if (options.StartActivityTrace)
            {
                var activitySource = new ActivitySource(nameof(AuditScope), typeof(AuditScope).Assembly.GetName().Version!.ToString());
                _activity = activitySource.StartActivity(_event.GetType().Name, ActivityKind.Internal, null, tags: new Dictionary<string, object>
                {
                    { nameof(AuditEvent), _event }
                });
            }
            if (options.IncludeActivityTrace)
            {
                _event.Activity = GetActivityTrace();
            }
#endif
            if (options.EventType != null)
            {
                _event.EventType = options.EventType;
            }
            
            if (_event.CustomFields == null)
            {
                _event.CustomFields = new Dictionary<string, object>();
            }

            ProcessExtraFields(options.ExtraFields);
            
            if (options.TargetGetter != null)
            {
                var targetValue = options.TargetGetter.Invoke();
                _event.Target = new AuditTarget
                {
                    Old = _dataProvider.CloneValue(targetValue, _event),
                    Type = targetValue?.GetType().GetFullTypeName() ?? "Object"
                };
            }
        }
#endregion

        #region Public Properties
        /// <summary>
        /// The current save mode. Useful on custom actions to determine the saving trigger.
        /// </summary>
        public SaveMode SaveMode => _saveMode;

        /// <summary>
        /// Indicates the change type
        /// </summary>
        public string EventType
        {
            get => _event.EventType;
            set => _event.EventType = value;
        }

        /// <summary>
        /// Gets the event related to this scope.
        /// </summary>
        public AuditEvent Event => _event;

        /// <summary>
        /// Gets the data provider for this AuditScope instance.
        /// </summary>
        public AuditDataProvider DataProvider => _dataProvider;

        /// <summary>
        /// Gets the current event ID, or NULL if not yet created.
        /// </summary>
        public object EventId => _eventId;

        /// <summary>
        /// Gets the creation policy for this scope.
        /// </summary>
        public EventCreationPolicy EventCreationPolicy => _creationPolicy;

        #endregion

        #region Private fields
        private readonly AuditScopeOptions _options;
        private SaveMode _saveMode;
        private readonly EventCreationPolicy _creationPolicy;
        private readonly AuditEvent _event;
        private object _eventId;
        private bool _disposed;
        private bool _ended;
        private readonly AuditDataProvider _dataProvider;
        private Func<object> _targetGetter;
#if NET6_0_OR_GREATER
        private readonly Activity _activity;
#endif
#endregion

        #region Public Methods
        /// <summary>
        /// Replaces the target object getter whose old/new value will be stored on the AuditEvent.Target property
        /// </summary>
        /// <param name="targetGetter">A function that returns the target</param>
        public void SetTargetGetter(Func<object> targetGetter)
        {
            _targetGetter = targetGetter;
            if (_targetGetter != null)
            {
                var targetValue = targetGetter.Invoke();
                _event.Target = new AuditTarget
                {
                    Old = _dataProvider.CloneValue(targetValue, _event),
                    Type = targetValue?.GetType().GetFullTypeName() ?? "Object"
                };
            }
            else
            {
                _event.Target = null;
            }
        }
        /// <summary>
        /// Add a textual comment to the event
        /// </summary>
        public void Comment(string text)
        {
            Comment(text, Array.Empty<object>());
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
        /// <param name="serialize">if set to <c>true</c> the value will be serialized immediately.</param>
        public void SetCustomField<TC>(string fieldName, TC value, bool serialize = false)
        {
            _event.CustomFields[fieldName] = serialize ? _dataProvider.CloneValue(value, _event) : value;
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
#if NET6_0_OR_GREATER
            _activity?.Dispose();
            _activity?.Source?.Dispose();
#endif
            Configuration.InvokeScopeCustomActions(ActionType.OnScopeDisposed, this);
        }

        /// <summary>
        /// Async version of the dispose method
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            await EndAsync();
#if NET6_0_OR_GREATER
            _activity?.Dispose();
            _activity?.Source?.Dispose();
#endif
            await Configuration.InvokeScopeCustomActionsAsync(ActionType.OnScopeDisposed, this, CancellationToken.None);
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
            if (IsEndedOrDisabled())
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
        /// Saves the event.
        /// </summary>
        private async Task EndAsync()
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            EndEvent();
            // process event creation/replacement
            if (_creationPolicy == EventCreationPolicy.InsertOnEnd || _creationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                await SaveEventAsync(true);
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd)
            {
                await SaveEventAsync(false);
            }
            _ended = true;
        }

        /// <summary>
        /// Manually Saves (insert/replace) the Event.
        /// Use this method to save (insert/replace) the event when CreationPolicy is set to Manual.
        /// </summary>
        public void Save()
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            EndEvent();
            SaveEvent();
        }

        /// <summary>
        /// Manually Saves (insert/replace) the Event asynchronously.
        /// Use this method to save (insert/replace) the event when CreationPolicy is set to Manual.
        /// </summary>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            EndEvent();
            await SaveEventAsync(false, cancellationToken);
        }

        /// <summary>
        /// Gets the event related to this scope of a known AuditEvent derived type. Returns null if the event is not of the specified type.
        /// </summary>
        /// <typeparam name="T">The AuditEvent derived type</typeparam>
        public T EventAs<T>() where T : AuditEvent
        {
            return _event as T;
        }
        
        #endregion

        #region Private Methods

#if NET6_0_OR_GREATER
        private AuditActivityTrace GetActivityTrace()
        {
            var activity = Activity.Current;

            if (activity == null)
            {
                return null;
            }

            var spanId = activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.Id,
                ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
                _ => null
            };

            var traceId = activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.RootId,
                ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
                _ => null
            };

            var parentId = activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.ParentId,
                ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
                _ => null
            };
            
            var result = new AuditActivityTrace()
            {
                StartTimeUtc = activity.StartTimeUtc,
                SpanId = spanId,
                TraceId = traceId,
                ParentId = parentId,
                Operation = activity.OperationName
            };

            if (activity.Tags.Any())
            {
                result.Tags = new List<AuditActivityTag>();
                foreach (var tag in activity.Tags)
                {
                    result.Tags.Add(new AuditActivityTag() { Key = tag.Key, Value = tag.Value });
                }
            }

            if (activity.Events.Any())
            {
                result.Events = new List<AuditActivityEvent>();
                foreach (var ev in activity.Events)
                {
                    result.Events.Add(new AuditActivityEvent() { Timestamp = ev.Timestamp, Name = ev.Name });
                }
            }

            return result;
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private AuditEventEnvironment GetEnvironmentInfo(AuditScopeOptions options)
        {
            var environment = new AuditEventEnvironment()
            {
                Culture = System.Globalization.CultureInfo.CurrentCulture.ToString(),
            };
            MethodBase callingMethod = options.CallingMethod;
            environment.UserName = Environment.UserName;
            environment.MachineName = Environment.MachineName;
            environment.DomainName = Environment.UserDomainName;
            if (callingMethod == null)
            {
                callingMethod = new StackFrame(3 + options.SkipExtraFrames).GetMethod();
            }
            if (options.IncludeStackTrace)
            {
                environment.StackTrace = new StackTrace(options.SkipExtraFrames, true).ToString();
            }
            if (callingMethod != null)
            {
                environment.CallingMethodName = (callingMethod.DeclaringType != null ? callingMethod.DeclaringType.FullName + "." : "") + callingMethod.Name + "()";
                environment.AssemblyName = callingMethod.DeclaringType?.GetTypeInfo().Assembly.FullName;
            }

            return environment;
        }

        /// <summary>
        /// Starts an audit scope
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal AuditScope Start()
        {
            _saveMode = SaveMode.InsertOnStart;
            // Execute custom on scope created actions
            Configuration.InvokeScopeCustomActions(ActionType.OnScopeCreated, this);

            // Process the event insertion (if applies)
            if (_options.IsCreateAndSave)
            {
                EndEvent();
                SaveEvent();
                _ended = true;
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd || _creationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                SaveEvent();
                _saveMode = _creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd ? SaveMode.ReplaceOnEnd : SaveMode.InsertOnEnd;
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnEnd)
            {
                _saveMode = SaveMode.InsertOnEnd;
            }
            else if (_creationPolicy == EventCreationPolicy.Manual)
            {
                _saveMode = SaveMode.Manual;
            }
            return this;
        }

        /// <summary>
        /// Starts an audit scope asynchronously
        /// </summary>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal async Task<AuditScope> StartAsync(CancellationToken cancellationToken = default)
        {
            _saveMode = SaveMode.InsertOnStart;
            // Execute custom on scope created actions
            await Configuration.InvokeScopeCustomActionsAsync(ActionType.OnScopeCreated, this, cancellationToken);

            // Process the event insertion (if applies)
            if (_options.IsCreateAndSave)
            {
                EndEvent();
                await SaveEventAsync(false, cancellationToken);
                _ended = true;
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd || _creationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd)
            {
                await SaveEventAsync(false, cancellationToken);
                _saveMode = _creationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd ? SaveMode.ReplaceOnEnd : SaveMode.InsertOnEnd;
            }
            else if (_creationPolicy == EventCreationPolicy.InsertOnEnd)
            {
                _saveMode = SaveMode.InsertOnEnd;
            }
            else if (_creationPolicy == EventCreationPolicy.Manual)
            {
                _saveMode = SaveMode.Manual;
            }
            return this;
        }
        private bool IsEndedOrDisabled()
        {
            if (!_ended && Configuration.AuditDisabled)
            {
                this.Discard();
            }
            return _ended;
        }
        // Update event info prior to save
        private void EndEvent()
        {
            var exception = GetCurrentException();
            _event.Environment.Exception = exception != null ? $"{exception.GetType().Name}: {exception.Message}" : null;
            _event.EndDate = Configuration.SystemClock.UtcNow;
            _event.Duration = Convert.ToInt32((_event.EndDate.Value - _event.StartDate).TotalMilliseconds);
            if (_targetGetter != null)
            {
                _event.Target.New = _dataProvider.CloneValue(_targetGetter.Invoke(), _event);
            }
        }

        private Exception GetCurrentException()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (PlatformHelper.IsRunningOnMono())
            {
                // Mono doesn't implement Marshal.GetExceptionCode() (https://github.com/mono/mono/blob/master/mcs/class/corlib/System.Runtime.InteropServices/Marshal.cs#L521)
                return null;
            }
            if (Marshal.GetExceptionCode() != 0)
            {
                return Marshal.GetExceptionForHR(Marshal.GetExceptionCode());
            }
            return null;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private void ProcessExtraFields(object extraFields)
        {
            if (extraFields == null)
            {
                return;
            }
            var props = extraFields.GetType().GetRuntimeProperties();
            foreach (var prop in props)
            {
                SetCustomField(prop.Name, prop.GetValue(extraFields, null));
            }
        }

        private void SaveEvent(bool forceInsert = false)
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            // Execute custom on event saving actions
            Configuration.InvokeScopeCustomActions(ActionType.OnEventSaving, this);
            if (IsEndedOrDisabled())
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
            // Execute custom after saving actions
            Configuration.InvokeScopeCustomActions(ActionType.OnEventSaved, this);
        }

        private async Task SaveEventAsync(bool forceInsert = false, CancellationToken cancellationToken = default)
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            // Execute custom on event saving actions
            await Configuration.InvokeScopeCustomActionsAsync(ActionType.OnEventSaving, this, cancellationToken);
            if (IsEndedOrDisabled())
            {
                return;
            }
            if (_eventId != null && !forceInsert)
            {
                await _dataProvider.ReplaceEventAsync(_eventId, _event, cancellationToken);
            }
            else
            {
                _eventId = await _dataProvider.InsertEventAsync(_event, cancellationToken);
            }
            // Execute custom after saving actions
            await Configuration.InvokeScopeCustomActionsAsync(ActionType.OnEventSaved, this, cancellationToken);
        }
        #endregion
    }
}
