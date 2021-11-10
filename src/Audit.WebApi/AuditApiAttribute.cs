#if ASP_NET
using System;
using Audit.Core;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net;
using System.Linq;

namespace Audit.WebApi
{
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
        /// Gets or sets a value indicating whether the output should include the Http request body string.
        /// </summary>
        public bool IncludeRequestBody { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the action arguments should be pre-serialized to the audit event.
        /// </summary>
        public bool SerializeActionParameters { get; set; }
        /// <summary>
        /// Gets or sets a string indicating the event type to use.
        /// Can contain the following placeholders:
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action method name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { get; set; }
        /// <summary>
        /// Gets or sets the class type that will be used as a context wrapper. 
        /// It must be a class implementing IContextWrapper with a public constructor receiving a single HttpRequestMessage parameter.
        /// Default is NULL to use the default ContextWrapper class.
        /// </summary>
        public Type ContextWrapperType { get; set; }
        
        private IContextWrapper GetContextWrapper(HttpRequestMessage request)
        {
            if (ContextWrapperType == null)
            {
                return new ContextWrapper(request);
            }
            else
            {
                return Activator.CreateInstance(ContextWrapperType, new object[] { request }) as IContextWrapper;
            }
        }
        
        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (Configuration.AuditDisabled || _adapter.IsActionIgnored(actionContext))
            {
                return;
            }
            await _adapter.BeforeExecutingAsync(actionContext, GetContextWrapper(actionContext.Request), IncludeHeaders, IncludeRequestBody, SerializeActionParameters, EventTypeName);
        }

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (Configuration.AuditDisabled || _adapter.IsActionIgnored(actionExecutedContext.ActionContext))
            {
                return;
            }
            await _adapter.AfterExecutedAsync(actionExecutedContext, GetContextWrapper(actionExecutedContext.Request), IncludeModelState, ShouldIncludeResponseBody(actionExecutedContext), IncludeResponseHeaders);
        }

        private bool ShouldIncludeResponseBody(HttpActionExecutedContext context)
        {
            if (context?.Response == null)
            {
                return IncludeResponseBody;
            }
            return ShouldIncludeResponseBody(context.Response.StatusCode);
        }

        internal bool ShouldIncludeResponseBody(HttpStatusCode responseStatusCode)
        {
            if (ExcludeResponseBodyFor != null)
            {
                if (ExcludeResponseBodyFor.Contains(responseStatusCode))
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
                return IncludeResponseBodyFor.Contains(responseStatusCode);
            }
            return IncludeResponseBody;
        }
    }
}
#endif