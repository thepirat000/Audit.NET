using System;
using System.Threading.Tasks;

namespace Audit.Core
{
    public interface IAuditScope : IDisposable
#if NETSTANDARD2_0 || NETSTANDARD2_1 || NET461 || NET5_0_OR_GREATER
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
#if NET45 || NETSTANDARD1_3
        ValueTask DisposeAsync();
#endif
        void Save();
        Task SaveAsync();
        void SetCustomField<TC>(string fieldName, TC value, bool serialize = false);
        void SetTargetGetter(Func<object> targetGetter);
    }
}