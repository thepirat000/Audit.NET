using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Audit.Core.Extensions
{
    /// <summary>
    /// Extension methods for Type type
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly Regex AnonymousTypeRegex = new Regex(@"<>f__AnonymousType\d*<");
        private const string AnonymousReplacementString = "AnonymousType<";

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
            var typeInfo = type.GetTypeInfo();
            var genericTypes = type.GenericTypeArguments;
            var name = Configuration.IncludeTypeNamespaces ? type.FullName : type.Name;
            if (!typeInfo.IsGenericType)
            {
                return name;
            }
            var sb = new StringBuilder();
            sb.Append(name.Substring(0, name.IndexOf("`")));

            sb.Append(genericTypes.Aggregate("<",
                delegate (string aggregate, Type t)
                {
                    return aggregate + (aggregate == "<" ? "" : ",") + GetFullTypeName(t);
                }
                ));
            sb.Append(">");
            return AnonymousTypeRegex.Replace(sb.ToString(), AnonymousReplacementString);
        }
    }
}
