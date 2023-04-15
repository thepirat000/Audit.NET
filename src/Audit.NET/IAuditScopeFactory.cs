using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Core
{
    public interface IAuditScopeFactory
    {
        /// <summary>
        /// Creates an audit scope with the given creation options.
        /// </summary>
        /// <param name="options">The Audit Scope creation options</param>
        IAuditScope Create(AuditScopeOptions options);
        /// <summary>
        /// Creates an audit scope with the given creation options.
        /// </summary>
        /// <param name="options">The Audit Scope creation options</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        Task<IAuditScope> CreateAsync(AuditScopeOptions options, CancellationToken cancellationToken = default);
    }
}