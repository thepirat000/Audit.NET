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
#pragma warning disable CS3002 // Activity not CLS-compliant

namespace Audit.Core
{
    /// <summary>
    /// Makes a code block auditable.
    /// </summary>
    public sealed partial class AuditScope : IAuditScope
    {
        private static readonly Lazy<ActivitySource> ActivitySource = new(() => new ActivitySource(typeof(AuditScope).FullName!, typeof(AuditScope).Assembly.GetName().Version!.ToString()));

        #region Constructors

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal AuditScope(AuditScopeOptions options)
        {
            _options = options;
            _creationPolicy = options.CreationPolicy ?? Configuration.CreationPolicy;
            _dataProvider = options.DataProvider ?? Configuration.DataProvider;
            _systemClock = options.SystemClock ?? Configuration.SystemClock;
            _targetGetter = options.TargetGetter;
            _items = options.Items ?? new Dictionary<string, object>();
            _event = options.AuditEvent ?? new AuditEvent();
            
            _event.SetScope(this);
            
            _event.StartDate = _systemClock.GetCurrentDateTime();

            if (options.IncludeTimestamps ?? Configuration.IncludeTimestamps)
            {
                _event.StartTimestamp = _systemClock.GetCurrentTimestamp();
            }

            _event.Environment = GetEnvironmentInfo(options);
            
            if (options.StartActivityTrace ?? Configuration.StartActivityTrace)
            {
                _activity = ActivitySource.Value.StartActivity(_event.GetType()!.Name);

                _activity?.SetCustomProperty(nameof(AuditEvent), _event);
            }

            if (options.IncludeActivityTrace ?? Configuration.IncludeActivityTrace)
            {
                _event.Activity = GetActivityTraceData();
            }

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

        /// <inheritdoc />
        public SaveMode SaveMode => _saveMode;

        /// <inheritdoc />
        public string EventType
        {
            get => _event.EventType;
            set => _event.EventType = value;
        }

        /// <inheritdoc />
        public AuditEvent Event => _event;

        /// <inheritdoc />
        public IAuditDataProvider DataProvider => _dataProvider;

        /// <inheritdoc />
        public object EventId => _eventId;

        /// <inheritdoc />
        public EventCreationPolicy EventCreationPolicy => _creationPolicy;

        /// <inheritdoc />
        public IDictionary<string, object> Items => _items;

        #endregion

        #region Private fields
        private readonly AuditScopeOptions _options;
        private SaveMode _saveMode;
        private readonly EventCreationPolicy _creationPolicy;
        private readonly AuditEvent _event;
        private object _eventId;
        private bool _disposed;
        private bool _ended;
        private readonly IAuditDataProvider _dataProvider;
        private readonly ISystemClock _systemClock;
        private Func<object> _targetGetter;
        private readonly IDictionary<string, object> _items;
        private readonly Activity _activity;

        #endregion

        #region Public Methods

        /// <inheritdoc />
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
        /// <inheritdoc />
        public void Comment(string text)
        {
            Comment(text, Array.Empty<object>());
        }

        /// <inheritdoc />
        public void Comment(string format, params object[] args)
        {
            if (_event.Comments == null)
            {
                _event.Comments = new List<string>();
            }
            _event.Comments.Add(string.Format(format, args));
        }

        /// <inheritdoc />
        public void SetCustomField<TC>(string fieldName, TC value, bool serialize = false)
        {
            _event.CustomFields[fieldName] = serialize ? _dataProvider.CloneValue(value, _event) : value;
        }

        /// <inheritdoc />
        public T GetItem<T>(string key)
        {
            if (_items.TryGetValue(key, out var value))
            {
                if (value is T obj)
                {
                    return obj;
                }
            }

            return default;
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
            _activity?.Dispose();
            Configuration.InvokeCustomActions(ActionType.OnScopeDisposed, this);
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
            _activity?.Dispose();
            await Configuration.InvokeCustomActionsAsync(ActionType.OnScopeDisposed, this, CancellationToken.None);
        }

        /// <inheritdoc />
        public void Discard()
        {
            // Mark as saved to ignore the saving
            _ended = true;
        }

        /// <summary>
        /// Ends the event.
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
        /// Ends the event.
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

        /// <inheritdoc />
        public void Save()
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            EndEvent();
            SaveEvent();
        }

        /// <inheritdoc />
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            EndEvent();
            await SaveEventAsync(false, cancellationToken);
        }

        /// <inheritdoc />
        public T EventAs<T>() where T : AuditEvent
        {
            return _event as T;
        }

        /// <inheritdoc />
        public Activity GetActivity()
        {
            return _activity;
        }

        #endregion

        #region Private Methods

        public static AuditActivityTrace GetActivityTraceData()
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private AuditEventEnvironment GetEnvironmentInfo(AuditScopeOptions options)
        {
            if (options.ExcludeEnvironmentInfo ?? Configuration.ExcludeEnvironmentInfo)
            {
                return null;
            }
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
            if (options.IncludeStackTrace ?? Configuration.IncludeStackTrace)
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
            Configuration.InvokeCustomActions(ActionType.OnScopeCreated, this);

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
            await Configuration.InvokeCustomActionsAsync(ActionType.OnScopeCreated, this, cancellationToken);

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
            if (_event.Environment != null)
            {
                var exception = GetCurrentException();
                _event.Environment.Exception = exception != null ? $"{exception.GetType().Name}: {exception.Message}" : null;
            }
            _event.EndDate = _systemClock.GetCurrentDateTime();

            if (_event.StartTimestamp.HasValue)
            {
                _event.EndTimestamp = _systemClock.GetCurrentTimestamp();
            }

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
            Configuration.InvokeCustomActions(ActionType.OnEventSaving, this);
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
            Configuration.InvokeCustomActions(ActionType.OnEventSaved, this);
        }

        private async Task SaveEventAsync(bool forceInsert = false, CancellationToken cancellationToken = default)
        {
            if (IsEndedOrDisabled())
            {
                return;
            }
            // Execute custom on event saving actions
            await Configuration.InvokeCustomActionsAsync(ActionType.OnEventSaving, this, cancellationToken);
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
            await Configuration.InvokeCustomActionsAsync(ActionType.OnEventSaved, this, cancellationToken);
        }
        #endregion
    }
}
