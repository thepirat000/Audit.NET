using System;
using Audit.Core;
using Audit.WebApi.ConfigurationApi;
#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Http;
#elif NET45
using System.Web;
using System.Net.Http;
#endif


namespace Audit.WebApi
{
    public static class ApiControllerExtensions
    {
#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
        /// <summary>
        /// Adds a global Audit Filter to the MVC filter chain. Use this method to add AuditApiGlobalFilter as a global filter.
        /// </summary>
        /// <param name="auditConfig">The audit configuration</param>
        /// <param name="mvcOptions">The MVC options</param>
        public static void AddAuditFilter(this Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions, Action<IAuditApiGlobalActionsSelector> auditConfig)
        {
            mvcOptions.Filters.Add(new AuditApiGlobalFilter(auditConfig));
        }
#else
        /// <summary>
        /// Adds a global Audit Filter to the MVC filter chain. Use this method to add AuditApiGlobalFilter as a global filter.
        /// </summary>
        /// <param name="auditConfig">The audit configuration</param>
        /// <param name="httpConfiguration">The HTTP configuration</param>
        public static void AddAuditFilter(this System.Web.Http.HttpConfiguration httpConfiguration, Action<IAuditApiGlobalActionsSelector> auditConfig)
        {
            httpConfiguration.Filters.Add(new AuditApiGlobalFilter(auditConfig));
        }
#endif

#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="apiController">The API controller.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this Microsoft.AspNetCore.Mvc.ControllerBase apiController)
        {
            return AuditApiAdapter.GetCurrentScope(apiController.HttpContext);
        }
#elif NET45
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="apiController">The API controller.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        /// <param name="contextWrapper">The context wrapper instance to use to provide the context. Default is NULL to use the default ContextWrapper.</param>
        public static AuditScope GetCurrentAuditScope(this System.Web.Http.ApiController apiController, IContextWrapper contextWrapper = null)
        {
            return AuditApiAdapter.GetCurrentScope(apiController.Request, contextWrapper);
        }
#endif

#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="httpContext">The http context to get the scope from.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        public static AuditScope GetCurrentAuditScope(this HttpContext httpContext)
        {
            return AuditApiAdapter.GetCurrentScope(httpContext);
        }
        /// <summary>
        /// Discards the Audit Scope related to the current context, if any.
        /// </summary>
        public static void DiscardCurrentAuditScope(this HttpContext httpContext)
        {
            AuditApiAdapter.DiscardCurrentScope(httpContext);
        }
#elif NET45
        /// <summary>
        /// Gets the current Audit Scope.
        /// </summary>
        /// <param name="httpRequest">The http request to get the scope from.</param>
        /// <returns>The current Audit Scope or NULL.</returns>
        /// <param name="contextWrapper">The context wrapper instance to use to provide the context. Default is NULL to use the default ContextWrapper.</param>
        public static AuditScope GetCurrentAuditScope(this HttpRequestMessage httpRequest, IContextWrapper contextWrapper = null)
        {
            return AuditApiAdapter.GetCurrentScope(httpRequest, contextWrapper);
        }
#endif
    }

}
