﻿using Audit.Core;

namespace Audit.Wcf.Client
{
    public static class AuditEventExtensions
    {
        /// <summary>
        /// Gets the WCF Client Event portion of the Audit Event for a given scope.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public static WcfClientAction GetWcfClientAction(this AuditScope auditScope)
        {
            return auditScope?.Event.GetWcfClientAction();
        }

        /// <summary>
        /// Gets the WCF Client Event portion of the Audit Event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public static WcfClientAction GetWcfClientAction(this AuditEvent auditEvent)
        {
            if (auditEvent is AuditEventWcfClient client)
            {
                return client.WcfClientEvent;
            }
            return auditEvent.CustomFields.TryGetValue("WcfClientEvent", out var field)
                ? Configuration.JsonAdapter.ToObject<WcfClientAction>(field)
                : null;
        }
    }
}
