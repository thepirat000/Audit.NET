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
#if NETSTANDARD1_6
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.Controller controller)
        {
            return AuditAttribute.GetCurrentScope(controller.HttpContext);
        }
#elif NET45 || NET40
        public static AuditScope GetCurrentAuditScope(this System.Web.Mvc.Controller controller)
        {
            return AuditAttribute.GetCurrentScope(controller.HttpContext);
        }
#endif
    }
}
