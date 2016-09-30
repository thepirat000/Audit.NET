using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Audit.Core.Extensions;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Describes an intercepted argument (input parameter, output paramater or result).
    /// </summary>
    public class AuditInterceptArgument
    {
        /// <summary>
        /// The argument index.
        /// </summary>
        [JsonProperty(Order = 1, NullValueHandling = NullValueHandling.Ignore)]
        public int? Index { get; set; }
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

        public AuditInterceptArgument(string name, Type type, object value, int index)
        {
            Name = name;
            Type = type.GetFullTypeName();
            Value = value;
            Index = index;
        }

        public AuditInterceptArgument(Type type, object value)
        {
            Type = type.GetFullTypeName();
            Value = value;
        }
    }
}