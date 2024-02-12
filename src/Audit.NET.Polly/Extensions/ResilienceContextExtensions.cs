using Audit.Core;
using Polly;

namespace Audit.Polly
{
    public static class ResilienceContextExtensions
    {
        /// <summary>
        /// Returns the AuditEvent from the Resilience Context
        /// </summary>
        public static AuditEvent? GetAuditEvent(this ResilienceContext context)
        {
            return context.Properties.GetValue(new ResiliencePropertyKey<AuditEvent?>("AuditEvent"), null);
        }
    }
}