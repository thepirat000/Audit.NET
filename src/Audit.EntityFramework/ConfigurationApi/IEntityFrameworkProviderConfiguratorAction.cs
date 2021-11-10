﻿using Audit.Core;
using System;
using System.Threading.Tasks;

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
        /// An asynchronous action to perform on the audit entity before saving it. This action is triggered for each entity being modified.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, Task> asyncAction);
        /// <summary>
        /// An action to perform on the audit entity before saving it. This action is triggered for each entity being modified.
        /// </summary>
        /// <param name="action">The action to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> action);
        /// <summary>
        /// An asynchronous action to perform on the audit entity before saving it. This action is triggered for each entity being modified.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task> asyncAction);
        /// <summary>
        /// A function to perform on the audit entity before saving it. 
        /// The function must return a boolean value indicating whether to include or not the entity on the audit log.
        /// </summary>
        /// <param name="function">The function to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity.
        /// Must return a boolean value indicating whether to include the entity.</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, bool> function);
        /// <summary>
        /// An asynchronous function to perform on the audit entity before saving it. 
        /// The function must return a boolean value indicating whether to include or not the entity on the audit log.
        /// </summary>
        /// <param name="asyncFunction">The asynchronous function to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity.
        /// Must return a boolean value indicating whether to include the entity.</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction(Func<AuditEvent, EventEntry, object, Task<bool>> asyncFunction);
        /// <summary>
        /// A function to perform on the audit entity before saving it. This is triggered for each entity being modified.
        /// The function must return a boolean value indicating whether to include or not the entity on the audit log.
        /// </summary>
        /// <param name="function">The function to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity.
        /// Must return a boolean value indicating whether to include the entity.</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, bool> function);
        /// <summary>
        /// A asynchronous function to perform on the audit entity before saving it. This is triggered for each entity being modified.
        /// The function must return a boolean value indicating whether to include or not the entity on the audit log.
        /// </summary>
        /// <param name="asyncFunction">The asynchronous function to perform on the audit entity before saving it. First parameter is the entire audit event, Second parameter is the entity modified entry and the third is the audit entity.
        /// Must return a boolean value indicating whether to include the entity.</param>
        IEntityFrameworkProviderConfiguratorExtra AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task<bool>> asyncFunction);

    }

}
