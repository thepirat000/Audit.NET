using Audit.Core;

namespace Audit.WebApi
{
    public static class ApiControllerExtensions
    {
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="apiController">The API controller.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
#if NETSTANDARD1_6 || NET451
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.Controller apiController)
        {
            return AuditApiAttribute.GetCurrentScope(apiController.HttpContext);
        }
#elif NET45
        public static AuditScope GetCurrentAuditScope(this System.Web.Http.ApiController apiController)
        {
            return AuditApiAttribute.GetCurrentScope(apiController.Request);
        }
#endif
    }
}
