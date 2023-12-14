using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using JsonProperty = Newtonsoft.Json.Serialization.JsonProperty;

namespace Audit.JsonNewtonsoftAdapter
{
    /// <summary>
    /// Newtonsoft.Json Contract Resolver that takes into account JsonExtensionDataAttribute and JsonIgnoreAttribute from System.Text.Json
    /// </summary>
    public class AuditContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            // Ignore properties marked with JsonExtensionData or JsonIgnore(Condition=Always) attributes (from System.Text.Json)
            properties = properties
                .Where(p => !p.AttributeProvider!.GetAttributes(true)
                    .Any(t => t switch
                    {
                        System.Text.Json.Serialization.JsonExtensionDataAttribute => true,
                        System.Text.Json.Serialization.JsonIgnoreAttribute jsonIgnoreAttr => jsonIgnoreAttr.Condition == JsonIgnoreCondition.Always,
                        _ => false
                    }))
                .ToList();

            return properties;
        }
        
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            if (contract.ExtensionDataGetter != null || contract.ExtensionDataSetter != null)
            {
                // Extension data getter/setter already configured (the object to serialize has Newtonsoft.Json.JsonExtensionData decorator)
                return contract;
            }

            var extensionDataProperty = objectType
                .GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<System.Text.Json.Serialization.JsonExtensionDataAttribute>(true) != null);

            var genericArguments = extensionDataProperty?.PropertyType.GetGenericArguments();

            if (extensionDataProperty == null || genericArguments.Length != 2)
            {
                // No Extension data property, or invalid property type
                return contract;
            }

            contract.ExtensionDataGetter = (o) =>
            {
                var extDataPropValue = extensionDataProperty.GetValue(o);
                
                return extDataPropValue switch
                {
                    IDictionary<string, object> dict => dict.ToDictionary(k => (object) k.Key, v => v.Value),
                    IDictionary<string, JsonElement> dict => dict.ToDictionary(k => (object) k.Key, v => (object)Newtonsoft.Json.Linq.JToken.Parse(v.Value.ToString())),
                    _ => null
                };
            };

            contract.ExtensionDataSetter = (o, key, value) =>
            {
                if (key.StartsWith("@"))
                {
                    return;
                }

                if (extensionDataProperty.GetValue(o) == null)
                {
                    // Create the extension data dictionary
                    var createType = typeof(Dictionary<,>).MakeGenericType(genericArguments[0], genericArguments[1]);
                    extensionDataProperty.SetValue(o, Activator.CreateInstance(createType));
                }

                // Add the value to the extension data object
                var propValue = (dynamic)extensionDataProperty.GetValue(o);
                if (genericArguments[1] == typeof(JsonElement))
                {
                    propValue![key] = value == null ? new JsonElement() : JsonDocument.Parse(value.ToString()).RootElement;
                }
                else
                {
                    propValue![key] = value == null ? null : JToken.FromObject(value);
                }
            };

            contract.ExtensionDataValueType = genericArguments[1];

            return contract;
        }
    }
}