#if NET45
using System.Web;
using Audit.Core;

namespace Audit.Mvc
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="controller">The MVC controller.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this System.Web.Mvc.Controller controller)
        {
            return AuditAttribute.GetCurrentScope(controller.HttpContext);
        }


        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="httpContext">The http context to get the scope from.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static IAuditScope GetCurrentAuditScope(this HttpContextBase httpContext)
        {
            return AuditAttribute.GetCurrentScope(httpContext);
        }
    }
}
#endif