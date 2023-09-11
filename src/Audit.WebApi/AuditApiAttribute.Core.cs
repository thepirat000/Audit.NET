#if ASP_CORE
using System.Linq;
using Audit.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Audit.WebApi
{
    /// <summary>
    /// This filter enable the audit on web api calls. 
    /// Mark with this attribute, the controllers and/or actions that needs to be audited.
    /// </summary>
    /// <remarks>If you need more granular control over the settings, use the AuditApiGlobal as a global filter instead of this attribute.</remarks>
    public class AuditApiAttribute : ActionFilterAttribute
    {
        private AuditApiAdapter _adapter = new AuditApiAdapter();

        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Response Headers.
        /// </summary>
        public bool IncludeResponseHeaders { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Request Headers.
        /// </summary>
        public bool IncludeHeaders { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include Model State information.
        /// </summary>
        public bool IncludeModelState { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Response body for all the requests.
        /// (Use the alternative IncludeResponseBodyFor/ExcludeResponseBodyFor to log the body only for certain response status codes).
        /// </summary>
        public bool IncludeResponseBody { get; set; }
        /// <summary>
        /// Gets or sets an array of status codes that conditionally indicates when the response body should be included.
        /// The response body is included only when the request return any of these status codes.
        /// When set to NULL, the IncludeResponseBody boolean is used to determine whether to include the response body or not. Exclude has precedence over Include.
        /// When to set to non-NULL, the IncludeResponseBody boolean is ignored.
        /// </summary>
        public HttpStatusCode[] IncludeResponseBodyFor { get; set; }
        /// <summary>
        /// Gets or sets an array of status codes that conditionally indicates when the response body should be excluded.
        /// The response body is included only when the request return a status code not in this array.
        /// When set to NULL, the IncludeResponseBodyFor or IncludeResponseBody are used to determine whether to include the response body or not. Exclude has precedence over Include.
        /// When to set to non-NULL, the IncludeResponseBody boolean is ignored.
        /// </summary>
        public HttpStatusCode[] ExcludeResponseBodyFor { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the Request Body should be read and included on the event.
        /// </summary>
        /// <remarks>
        /// When IncludeRequestBody is set to true and you are not using a [FromBody] parameter (i.e.reading the request body directly from the Request)
        /// make sure you enable rewind on the request body stream, otherwise the controller won't be able to read the request body since, by default, 
        /// it's a forward-only stream that can be read only once. 
        /// </remarks>
        public bool IncludeRequestBody { get; set; }
        /// <summary>
        /// Gets or sets a string indicating the event type to use.
        /// Can contain the following placeholders:
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action method name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the action arguments should be pre-serialized to the audit event.
        /// </summary>
        public bool SerializeActionParameters { get; set; }
        /// <summary>
        /// Gets or sets an array of status codes that conditionally indicates when the Audit scope should be discarded.
        /// The Audit action is triggered only when the request return a status code not in this array.
        /// </summary>
        public HttpStatusCode[] DiscardFor { get; set; }

        public AuditApiAttribute()
        {
            this.Order = int.MinValue;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (Configuration.AuditDisabled || _adapter.ActionIgnored(context))
            {
                await next.Invoke();
                return;
            }
            await _adapter.BeforeExecutingAsync(context, IncludeHeaders, IncludeRequestBody, SerializeActionParameters, EventTypeName);
            var actionExecutedContext = await next.Invoke();

            if (ShouldDiscardForResponseStatus(actionExecutedContext))
            {
                AuditApiAdapter.DiscardCurrentScope(actionExecutedContext.HttpContext);
                return;
            }
            await _adapter.AfterExecutedAsync(actionExecutedContext, IncludeModelState, ShouldIncludeResponseBody(actionExecutedContext), IncludeResponseHeaders);
        }

        private bool ShouldDiscardForResponseStatus(ActionExecutedContext context) => ShouldDiscardForResponseStatus(GetStatusCode(context));

        private bool ShouldDiscardForResponseStatus(HttpStatusCode statusCode) => DiscardFor != null && DiscardFor.Contains(statusCode);

        private bool ShouldIncludeResponseBody(ActionExecutedContext context)
        {
            var statusCode = GetStatusCode(context);
            return ShouldIncludeResponseBody(statusCode);
        }

        internal bool ShouldIncludeResponseBody(HttpStatusCode statusCode)
        {
            if (ExcludeResponseBodyFor != null)
            {
                if (ExcludeResponseBodyFor.Contains(statusCode))
                {
                    return false;
                }
                if (IncludeResponseBodyFor == null)
                {
                    // there is an exclude not matched, and there is NO include list
                    return true;
                }
            }
            if (IncludeResponseBodyFor != null)
            {
                return IncludeResponseBodyFor.Contains(statusCode);
            }
            return IncludeResponseBody;
        }

        private HttpStatusCode GetStatusCode(ActionExecutedContext context)
        {
            var statusCode = context.Result is ObjectResult && (context.Result as ObjectResult).StatusCode.HasValue
                ? (context.Result as ObjectResult).StatusCode.Value
                : context.Result is StatusCodeResult
                    ? (context.Result as StatusCodeResult).StatusCode
                    : context.HttpContext.Response.StatusCode;
            return (HttpStatusCode)statusCode;
        }

    }
}
#endif