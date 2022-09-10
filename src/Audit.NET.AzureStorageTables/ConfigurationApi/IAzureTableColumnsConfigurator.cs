using Audit.Core;
using System;
using System.Collections.Generic;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    /// <summary>
    /// Defines a fluent configuration for the entity columns to be stored on an Azure Table
    /// </summary>
    public interface IAzureTableColumnsConfigurator
    {
        /// <summary>
        /// Sets the columns (properties) values from a dictionary of strings and objects. 
        /// Key is the column name. Value is the column value (Value must be of a simple type or convertible to string).
        /// </summary>
        /// <param name="dictionaryBuilder">A function that takes the audit event and return the columns dictionary</param>
        void FromDictionary(Func<AuditEvent, IDictionary<string, object>> dictionaryBuilder);
        /// <summary>
        /// Sets the columns (properties) values from an object or an anonymous object. 
        /// The object properties Values must be of a simple type or convertible to string.
        /// </summary>
        /// <param name="objectBuilder">A function that takes the audit event and return the columns object</param>
        void FromObject(Func<AuditEvent, object> objectBuilder);
    }
}
