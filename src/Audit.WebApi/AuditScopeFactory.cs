using Audit.Core;
using Audit.Core.Providers;
using System.Runtime.CompilerServices;

namespace Audit.WebApi
{
    internal class AuditScopeFactory
    {
        /// <summary>
        /// Creates a no op audit scope
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AuditScope CreateNoOp() => AuditScope.Create(new AuditScopeOptions
        {
            DataProvider = new NullDataProvider()
        });
    }
}
