using Audit.Core;
using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Define the Entity to Audit-Entity mapping
    /// </summary>
    public interface IAuditEntityMapping
    {
        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity type specified on <typeparamref name="TAuditEntity"/>
        /// </summary>
        /// <param name="entityAction">An action to perform on the audit entity of type <typeparamref name="TAuditEntity"/> before saving it</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        /// <typeparam name="TAuditEntity">The entity type that holds the audit of the type audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<AuditEvent, EventEntry, TAuditEntity> entityAction);
        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity type specified on <typeparamref name="TAuditEntity"/>
        /// </summary>
        /// <param name="entityAction">An action to perform on the audit entity of type <typeparamref name="TAuditEntity"/> before saving it</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        /// <typeparam name="TAuditEntity">The entity type that holds the audit of the type audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Action<TSourceEntity, TAuditEntity> entityAction);
        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity type specified on <typeparamref name="TAuditEntity"/>
        /// </summary>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        /// <typeparam name="TAuditEntity">The entity type that holds the audit of the type audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity, TAuditEntity>();
        /// <summary>
        /// Defines a common action to perform to all the audit entities before saving. 
        /// </summary>
        /// <param name="entityAction">A default action to perform on the audit entity before saving it</param>
        void AuditEntityAction(Action<AuditEvent, EventEntry, object> entityAction);
        /// <summary>
        /// Defines a common action to perform to all the audit entities before saving. 
        /// </summary>
        /// <param name="entityAction">A default action to perform on the audit entity before saving it</param>
        /// <typeparam name="T">The audit entity type</typeparam>
        void AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> entityAction);
    }
}
