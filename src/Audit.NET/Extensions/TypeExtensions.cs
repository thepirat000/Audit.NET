using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Audit.Core.Extensions
{
    /// <summary>
    /// Extension methods for Type type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets the complete type name, including pretty printing for generic types.
        /// </summary>
        /// <param name="type">The type</param>
        public static string GetFullTypeName(this Type type)
        {
            if (type == null)
            {
                return null;
            }
#if NET40
            var typeInfo = type;
            var genericTypes = type.GetGenericArguments();
#else
            var typeInfo = type.GetTypeInfo();
            var genericTypes = type.GenericTypeArguments;
#endif
            if (!typeInfo.IsGenericType)
            {
                return type.Name;
            }
            var sb = new StringBuilder();
            sb.Append(type.Name.Substring(0, type.Name.LastIndexOf("`")));

            sb.Append(genericTypes.Aggregate("<",
                delegate (string aggregate, Type t)
                {
                    return aggregate + (aggregate == "<" ? "" : ",") + GetFullTypeName(t);
                }
                ));
            sb.Append(">");
            return sb.ToString();
        }
    }
}
