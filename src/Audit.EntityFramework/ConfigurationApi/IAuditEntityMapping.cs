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
        /// <param name="entityAction">A function to perform on the audit entity of type <typeparamref name="TAuditEntity"/> before saving it. 
        /// Must return a boolean indicating whether to include the entity on the audit logs.</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        /// <typeparam name="TAuditEntity">The entity type that holds the audit of the type audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Func<AuditEvent, EventEntry, TAuditEntity, bool> entityAction);

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
        /// <param name="entityAction">An action to perform on the audit entity of type <typeparamref name="TAuditEntity"/> before saving it
        /// Must return a boolean indicating whether to include the entity on the audit logs.</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        /// <typeparam name="TAuditEntity">The entity type that holds the audit of the type audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity, TAuditEntity>(Func<TSourceEntity, TAuditEntity, bool> entityAction);
        
        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity type specified on <typeparamref name="TAuditEntity"/>
        /// </summary>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        /// <typeparam name="TAuditEntity">The entity type that holds the audit of the type audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity, TAuditEntity>();

        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity returned on first parameter />
        /// </summary>
        /// <param name="mapper">The mapper function that takes the event entry information and returns the audit entity type</param>
        /// <param name="entityAction">An action to perform on the audit entity before saving it. Return true to include the entity, or false otherwise.</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity>(Func<EventEntry, Type> mapper, Func<AuditEvent, EventEntry, object, bool> entityAction);

        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity returned on first parameter />
        /// </summary>
        /// <param name="mapper">The mapper function that takes the event entry information and returns the audit entity type</param>
        /// <param name="entityAction">An action to perform on the audit entity before saving it</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity>(Func<EventEntry, Type> mapper, Action<AuditEvent, EventEntry, object> entityAction);

        /// <summary>
        /// Maps the entity type specified on <typeparamref name="TSourceEntity"/> to the audit entity returned on first parameter />
        /// </summary>
        /// <param name="mapper">The mapper function that takes the event entry information and returns the audit entity type</param>
        /// <typeparam name="TSourceEntity">The entity type to be audited</typeparam>
        IAuditEntityMapping Map<TSourceEntity>(Func<EventEntry, Type> mapper);

        /// <summary>
        /// Defines a common action to perform to all the audit entities before saving. 
        /// </summary>
        /// <param name="entityAction">A default action to perform on the audit entity before saving it</param>
        void AuditEntityAction(Action<AuditEvent, EventEntry, object> entityAction);

        /// <summary>
        /// Defines a common action to perform to all the audit entities before saving. 
        /// </summary>
        /// <param name="entityAction">A default action to perform on the audit entity before saving it
        /// Must return a boolean indicating whether to include the entity on the audit logs.</param>
        void AuditEntityAction(Func<AuditEvent, EventEntry, object, bool> entityAction);
        
        /// <summary>
        /// Defines a common action to perform to all the audit entities before saving. 
        /// </summary>
        /// <param name="entityAction">A default action to perform on the audit entity before saving it</param>
        /// <typeparam name="T">The audit entity type</typeparam>
        void AuditEntityAction<T>(Action<AuditEvent, EventEntry, T> entityAction);

        /// <summary>
        /// Defines a common action to perform to all the audit entities before saving. 
        /// </summary>
        /// <param name="entityAction">A default action to perform on the audit entity before saving it
        /// Must return a boolean indicating whether to include the entity on the audit logs.</param>
        /// <typeparam name="T">The audit entity type</typeparam>
        void AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, bool> entityAction);
    }
}
