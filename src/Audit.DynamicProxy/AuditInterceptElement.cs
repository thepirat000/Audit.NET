using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Describes an intercepted argument (input parameter, output paramater or result).
    /// </summary>
    public class AuditInterceptArgument
    {
        /// <summary>
        /// The argument name.
        /// </summary>
        [JsonProperty(Order = 10, NullValueHandling = NullValueHandling.Ignore)]
        public object Name { get; set; }
        /// <summary>
        /// The argument type.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string Type { get; set; }
        /// <summary>
        /// The argument value.
        /// </summary>
        [JsonProperty(Order = 30)]
        public object Value { get; set; }
        /// <summary>
        /// The argument output value.
        /// </summary>
        [JsonProperty(Order = 40, NullValueHandling = NullValueHandling.Ignore)]
        public object OutputValue { get; set; }

        public AuditInterceptArgument(object value)
        {
            Type = GetFullTypeName(value?.GetType());
            Value = value;
        }

        public AuditInterceptArgument(string name, Type type, object value)
        {
            Name = name;
            Type = GetFullTypeName(type);
            Value = value;
        }

        public AuditInterceptArgument(Type type, object value)
        {
            Type = GetFullTypeName(type);
            Value = value;
        }

        // TODO: Add this to the WCF library, etc
        private static string GetFullTypeName(Type t)
        {
            if (t == null)
            {
                return null;
            }
            if (!t.GetTypeInfo().IsGenericType)
            {
                return t.Name;
            }
            var sb = new StringBuilder();
            sb.Append(t.Name.Substring(0, t.Name.LastIndexOf("`")));
            sb.Append(t.GetGenericArguments().Aggregate("<",
                delegate (string aggregate, Type type)
                {
                    return aggregate + (aggregate == "<" ? "" : ",") + GetFullTypeName(type);
                }
                ));
            sb.Append(">");
            return sb.ToString();
        }
    }
}