using System.Linq;
using Audit.Core;
using System;
using System.Reflection;
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
#elif NET45
using System.Data.Entity.Core.Objects;
#endif

namespace Audit.EntityFramework.Providers
{
    /// <summary>
    /// Store the audits logs in the same EntityFramework model as the audited entities.
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - AuditTypeMapper: A function that maps an entity type to its audited type (i.e. Order -> OrderAudit, etc)
    /// - AuditEntityAction: An action to perform on the audit entity before saving it
    /// - IgnoreMatchedProperties: Set to true to avoid the property values copy from the entity to the audited entity (default is true)
    /// </remarks>
    public class EntityFrameworkDataProvider : AuditDataProvider
    {
        private bool _ignoreMatchedProperties;
        private Func<Type, Type> _auditTypeMapper;
        private Action<AuditEvent, EventEntry, object> _auditEntityAction;

        public Func<Type, Type> AuditTypeMapper
        {
            get { return _auditTypeMapper; }
            set { _auditTypeMapper = value; }
        }

        public Action<AuditEvent, EventEntry, object> AuditEntityAction
        {
            get { return _auditEntityAction; }
            set { _auditEntityAction = value; }
        }

        public bool IgnoreMatchedProperties
        {
            get { return _ignoreMatchedProperties; }
            set { _ignoreMatchedProperties = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            bool save = false;
            var efEvent = auditEvent as AuditEventEntityFramework;
            var dbContext = efEvent.EntityFrameworkEvent.DbContext;
            foreach(var entry in efEvent.EntityFrameworkEvent.Entries)
            {
                Type type;
#if NETSTANDARD2_0
                IEntityType definingType = entry.Entry.Metadata.DefiningEntityType ?? dbContext.Model.FindEntityType(entry.Entry.Entity.GetType());
                type = definingType?.ClrType;
#elif NETSTANDARD1_5 || NET461
                IEntityType definingType = dbContext.Model.FindEntityType(entry.Entry.Entity.GetType());
                type = definingType?.ClrType;
#else
                type = ObjectContext.GetObjectType(entry.Entry.Entity.GetType());
#endif
                if (type != null)
                {
                    var mappedType = _auditTypeMapper?.Invoke(type);
                    if (mappedType != null)
                    {
                        var auditEntity = CreateAuditEntity(type, mappedType, entry);
                        _auditEntityAction?.Invoke(efEvent, entry, auditEntity);
#if NET45
                        dbContext.Set(mappedType).Add(auditEntity);
#else
                        dbContext.Add(auditEntity);
#endif
                        save = true;
                    }
                }
            }
            if (save)
            {
                if (dbContext is AuditDbContext)
                {
                    (dbContext as AuditDbContext).SaveChangesBypassAudit();
                }
                else
                {
                    dbContext.SaveChanges();
                }
            }
            return null;
        }

        private object CreateAuditEntity(Type definingType, Type auditType, EventEntry entry)
        {
            var entity = entry.Entry.Entity;
            var auditEntity = Activator.CreateInstance(auditType);
            if (!_ignoreMatchedProperties)
            {
                var auditFields = auditType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(k => k.Name);
                var entityFields = definingType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in entityFields.Where(af => auditFields.ContainsKey(af.Name)))
                {
                    var value = field.GetValue(entity);
                    auditFields[field.Name].SetValue(auditEntity, value);
                }
            }
            return auditEntity;
        }
    }
}
