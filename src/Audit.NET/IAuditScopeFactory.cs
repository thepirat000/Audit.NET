using System;
using System.Threading.Tasks;

namespace Audit.Core
{
    public interface IAuditScopeFactory
    {
        /// <summary>
        /// Creates an audit scope with the given creation options.
        /// </summary>
        IAuditScope Create(AuditScopeOptions options);
        /// <summary>
        /// Creates an audit scope with the given creation options.
        /// </summary>
        Task<IAuditScope> CreateAsync(AuditScopeOptions options);
    }
}