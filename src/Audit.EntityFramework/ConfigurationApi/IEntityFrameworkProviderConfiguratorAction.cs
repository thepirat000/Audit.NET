using Audit.Core;
using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public interface IEntityFrameworkProviderConfiguratorAction
    {
        /// <summary>
        /// An action to perform on the audit entity before saving it. This action is triggered for each entity being modified.
        /// </summary>
        /// <param name="action">The action to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Action<AuditEvent, EventEntry, object> action);
        /// <summary>
        /// An action to perform on the audit entity before saving it. This action is triggered for each entity being modified.
        /// </summary>
        /// <param name="action">The action to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> action);
    }

}
