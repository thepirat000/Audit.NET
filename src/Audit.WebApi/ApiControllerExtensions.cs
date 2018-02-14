using System.Web;
using Audit.Core;
#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Http;
#elif NET45
using System.Net.Http;
#endif

namespace Audit.WebApi
{
    public static class ApiControllerExtensions
    {
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="apiController">The API controller.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
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
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="httpContext">The http context to get the scope from.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
        public static AuditScope GetCurrentAuditScope(this HttpContext httpContext)
        {
            return AuditApiAttribute.GetCurrentScope(httpContext);
        }
#elif NET45
        public static AuditScope GetCurrentAuditScope(this HttpRequestMessage httpContext)
        {
            return AuditApiAttribute.GetCurrentScope(httpContext);
        }
#endif
    }

}
