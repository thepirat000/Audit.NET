using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Audit.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureTableColumnsConfigurator : IAzureTableColumnsConfigurator
    {
        internal Func<AuditEvent, IDictionary<string, EntityProperty>> _propsBuilder = null;

        public void FromDictionary(Func<AuditEvent, IDictionary<string, object>> dictionaryBuilder)
        {
            _propsBuilder = ev => dictionaryBuilder?.Invoke(ev)?.ToDictionary(k => k.Key, v => EntityProperty.CreateEntityPropertyFromObject(v.Value));
        }

        public void FromObject(Func<AuditEvent, object> objectBuilder)
        {
            _propsBuilder = ev => GetProperties(objectBuilder.Invoke(ev));
        }

        private IDictionary<string, EntityProperty> GetProperties(object values)
        {
            if (values != null)
            {
                var props = values.GetType().GetProperties();
                return props.ToDictionary(k => k.Name, v => EntityProperty.CreateEntityPropertyFromObject(v.GetValue(values, null)));
            }
            return null;
        }
    }
}