using System;
using System.Threading.Tasks;

namespace Audit.Core
{
    public interface IAuditScope : IDisposable
#if NETSTANDARD2_1
        , IAsyncDisposable
#endif
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
#if !NETSTANDARD2_1
        Task DisposeAsync();
#endif
        void Save();
        Task SaveAsync();
        void SetCustomField<TC>(string fieldName, TC value, bool serialize = false);
        void SetTargetGetter(Func<object> targetGetter);
    }
}