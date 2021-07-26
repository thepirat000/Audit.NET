using System;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public int? Index { get; set; }
        /// <summary>
        /// The argument name.
        /// </summary>
        public object Name { get; set; }
        /// <summary>
        /// The argument type.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The argument value.
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// The argument output value.
        /// </summary>
        public object OutputValue { get; set; }

        public AuditInterceptArgument()
        {
        }

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