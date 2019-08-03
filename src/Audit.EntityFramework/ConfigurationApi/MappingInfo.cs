using Audit.Core;
using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    internal class MappingInfo
    {
        /// <summary>
        /// The Target (Audit) type 
        /// </summary>
        public Type TargetType
        {
            set
            {
                TargetTypeMapper = _ => value;
            }
        }
       
        /// <summary>
        /// The Target (Audit) type mapper
        /// </summary>
        public Func<EventEntry, Type> TargetTypeMapper { get; set; }

        /// <summary>
        /// The Action to execute for this mapping.
        /// </summary>
        public Func<AuditEvent, EventEntry, object, bool> Action { get; set; }
    }

}
