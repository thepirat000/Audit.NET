using System.Web.Http;
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
        public static AuditScope GetCurrentAuditScope(this ApiController apiController)
        {
            return AuditApiAttribute.GetCurrentScope(apiController.Request);
        }
    }
}