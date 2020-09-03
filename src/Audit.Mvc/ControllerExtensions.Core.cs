#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
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
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.ControllerBase controller)
        {
            return AuditAttribute.GetCurrentScope(controller.HttpContext);
        }

#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0
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