using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.ModelBinding;
using Audit.Core;

namespace Audit.WebApi
{
    public class AuditApiAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Request Headers.
        /// </summary>
        public bool IncludeHeaders { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include Model State information.
        /// </summary>
        public bool IncludeModelState { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Response text.
        /// </summary>
        public bool IncludeResponseBody { get; set; }
        /// <summary>
        /// Gets or sets a string indicating the event type to use.
        /// Can contain the following placeholders:
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action method name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { get; set; }

        private const string AuditApiActionKey = "__private_AuditApiAction__";
        private const string AuditApiScopeKey = "__private_AuditApiScope__";

        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var httpContext = GetHttpContext(request);

            var auditAction = new AuditApiAction
            {
                UserName = actionContext.RequestContext.Principal?.Identity?.Name,
                IpAddress = GetClientIp(request),
                RequestUrl = request.RequestUri.AbsoluteUri,
                HttpMethod = actionContext.Request.Method.Method,
                FormVariables = ToDictionary(httpContext.Request.Form), //TODO CHECK THIS!
                Headers = IncludeHeaders ? ToDictionary(request.Headers) : null,
                ActionName = actionContext.ActionDescriptor.ActionName,
                ControllerName = actionContext.ActionDescriptor.ControllerDescriptor.ControllerName,
                ActionParameters = actionContext.ActionArguments
            };
            var eventType = (EventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditScope = AuditScope.Create(eventType, null, new { Action = auditAction }, EventCreationPolicy.Manual);
            httpContext.Items[AuditApiActionKey] = auditAction;
            httpContext.Items[AuditApiScopeKey] = auditScope;
        }

        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        /// <param name="actionExecutedContext">The action executed context.</param>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var httpContext = GetHttpContext(actionExecutedContext.Request);
            var auditAction = httpContext.Items[AuditApiActionKey] as AuditApiAction;
            var auditScope = httpContext.Items[AuditApiScopeKey] as AuditScope;
            if (auditAction != null && auditScope != null)
            {
                auditAction.Exception = GetExceptionInfo(actionExecutedContext.Exception);
                auditAction.ModelStateErrors = IncludeModelState ? GetModelStateErrors(actionExecutedContext.ActionContext.ModelState) : null;
                auditAction.ModelStateValid = IncludeModelState ? actionExecutedContext.ActionContext.ModelState?.IsValid : null;
                if (actionExecutedContext.Response != null)
                {
                    auditAction.ResponseStatus = actionExecutedContext.Response.ReasonPhrase;
                    auditAction.ResponseStatusCode = (int) actionExecutedContext.Response.StatusCode;
                    if (IncludeResponseBody)
                    {
                        var objContent = actionExecutedContext.Response.Content as ObjectContent;
                        auditAction.ResponseBody = objContent != null 
                            ? new { Type = objContent.ObjectType.Name, Value = objContent.Value } 
                            : (object)actionExecutedContext.Response.Content?.ReadAsStringAsync().Result;
                    }
                }
                // Replace the Action field and save
                auditScope.SetCustomField("Action", auditAction);
                auditScope.Save();
            }
        }

        private static IDictionary<string, string> ToDictionary(HttpRequestHeaders col)
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var k in col)
            {
                dict.Add(k.Key, string.Join(", ", k.Value));
            }
            return dict;
        }

        private static IDictionary<string, string> ToDictionary(NameValueCollection col)
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var k in col.AllKeys)
            {
                dict.Add(k, col[k]);
            }
            return dict;
        }

        private static Dictionary<string, string> GetModelStateErrors(ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                return null;
            }
            var dict = new Dictionary<string, string>();
            foreach (var state in modelState)
            {
                if (state.Value.Errors.Count > 0)
                {
                    dict.Add(state.Key, string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }
            return dict.Count > 0 ? dict : null;
        }

        private string GetClientIp(HttpRequestMessage request)
        {
            return GetHttpContext(request).Request.UserHostAddress;
        }

        private static string GetExceptionInfo(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }
            string exceptionInfo = $"({exception.GetType().Name}) {exception.Message}";
            Exception inner = exception;
            while ((inner = inner.InnerException) != null)
            {
                exceptionInfo += " -> " + inner.Message;
            }
            return exceptionInfo;
        }

        private static HttpContextWrapper GetHttpContext(HttpRequestMessage request)
        {
            HttpContextWrapper context = null;
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                object obj;
                request.Properties.TryGetValue("MS_HttpContext", out obj);
                context = obj as HttpContextWrapper;
            }
            return context ?? new HttpContextWrapper(HttpContext.Current);
        }
    }
}
