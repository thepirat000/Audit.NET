#if NET45
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
    }
}
#endif