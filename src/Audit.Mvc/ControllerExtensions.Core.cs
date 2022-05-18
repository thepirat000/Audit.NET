#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451 || NET5_0
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


#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NET5_0
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="page">The razor page.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.RazorPages.PageModel page)
        {
            return AuditAttribute.GetCurrentScope(page.HttpContext);
        }
#endif
    }
}
#endif