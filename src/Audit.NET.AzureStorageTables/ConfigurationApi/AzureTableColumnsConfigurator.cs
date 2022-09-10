using Audit.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public class AzureTableColumnsConfigurator : IAzureTableColumnsConfigurator
    {
        internal Func<AuditEvent, IDictionary<string, object>> _propsBuilder = null;

        public void FromDictionary(Func<AuditEvent, IDictionary<string, object>> dictionaryBuilder)
        {
            _propsBuilder = ev => dictionaryBuilder?.Invoke(ev)?.ToDictionary(k => k.Key, v => v.Value);
        }

        public void FromObject(Func<AuditEvent, object> objectBuilder)
        {
            _propsBuilder = ev => GetProperties(objectBuilder.Invoke(ev));
        }

        private IDictionary<string, object> GetProperties(object values)
        {
            if (values != null)
            {
                var props = values.GetType().GetProperties();
                return props.ToDictionary(k => k.Name, v => v.GetValue(values, null));
            }
            return null;
        }
    }
}
