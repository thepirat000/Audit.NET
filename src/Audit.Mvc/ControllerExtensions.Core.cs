#if ASP_CORE
using Audit.Core;
using Microsoft.AspNetCore.Http;

namespace Audit.Mvc
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="controller">The MVC controller.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.ControllerBase controller)
        {
            return AuditAttribute.GetCurrentScope(controller.HttpContext);
        }

        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this HttpContext httpContext)
        {
            return AuditAttribute.GetCurrentScope(httpContext);
        }
        
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="page">The razor page.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.RazorPages.PageModel page)
        {
            return AuditAttribute.GetCurrentScope(page.HttpContext);
        }
    }
}
#endif