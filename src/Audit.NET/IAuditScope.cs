using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    public interface IAuditScope : IDisposable, IAsyncDisposable
    {
        AuditDataProvider DataProvider { get; }
        AuditEvent Event { get; }
        EventCreationPolicy EventCreationPolicy { get; }
        object EventId { get; }
        string EventType { get; set; }
        SaveMode SaveMode { get; }
        void Comment(string text);
        void Comment(string format, params object[] args);
        void Discard();
        void Save();
        Task SaveAsync(CancellationToken cancellationToken = default);
        void SetCustomField<TC>(string fieldName, TC value, bool serialize = false);
        void SetTargetGetter(Func<object> targetGetter);
        T EventAs<T>() where T : AuditEvent;
    }
}