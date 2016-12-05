#if NETSTANDARD1_6
using System;
using System.Collections.Generic;
using System.Linq;
using Audit.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Audit.Core.Extensions;

namespace Audit.WebApi
{
    public class AuditApiAttribute : ActionFilterAttribute
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
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var httpContext = actionContext.HttpContext;
            var actionDescriptior = actionContext.ActionDescriptor as ControllerActionDescriptor;
            var auditAction = new AuditApiAction
            {
                UserName = httpContext.User?.Identity.Name,
                IpAddress = httpContext.Connection?.RemoteIpAddress.ToString(),
                RequestUrl = string.Format("{0}://{1}{2}", httpContext.Request.Scheme, httpContext.Request.Host, httpContext.Request.Path),
                HttpMethod = actionContext.HttpContext.Request.Method,
                FormVariables = httpContext.Request.HasFormContentType ? ToDictionary(httpContext.Request.Form) : null, 
                Headers = IncludeHeaders ? ToDictionary(httpContext.Request.Headers) : null,
                ActionName = actionDescriptior != null ? actionDescriptior.ActionName : actionContext.ActionDescriptor.DisplayName,
                ControllerName = actionDescriptior != null ? actionDescriptior.ControllerName : null,
                ActionParameters = actionContext.ActionArguments
            };
            var eventType = (EventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditEventAction = new AuditEventWebApi()
            {
                Action = auditAction
            };
            var auditScope = AuditScope.Create(eventType, null, null, Configuration.CreationPolicy, null, auditEventAction);
            httpContext.Items[AuditApiActionKey] = auditAction;
            httpContext.Items[AuditApiScopeKey] = auditScope;
        }

        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        /// <param name="actionExecutedContext">The action executed context.</param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var httpContext = context.HttpContext;
            var auditAction = httpContext.Items[AuditApiActionKey] as AuditApiAction;
            var auditScope = httpContext.Items[AuditApiScopeKey] as AuditScope;
            if (auditAction != null && auditScope != null)
            {
                auditAction.Exception = context.Exception.GetExceptionInfo();
                auditAction.ModelStateErrors = IncludeModelState ? GetModelStateErrors(context.ModelState) : null;
                auditAction.ModelStateValid = IncludeModelState ? context.ModelState?.IsValid : null;
                auditAction.ResponseBodyType = context.Result?.GetType().Name;
                if (context.HttpContext.Response != null && context.Result != null)
                {
                    auditAction.ResponseStatus = context.HttpContext.Response.StatusCode.ToString();
                    auditAction.ResponseStatusCode = context.HttpContext.Response.StatusCode;
                    if (IncludeResponseBody)
                    {
                        switch(auditAction.ResponseBodyType)
                        {
                            case nameof(ObjectResult):
                                auditAction.ResponseBody = (context.Result as ObjectResult).Value;
                                break;
                            case nameof(StatusCodeResult):
                                auditAction.ResponseBody = string.Format("StatusCode ({0})", (context.Result as StatusCodeResult).StatusCode);
                                break;
                            case nameof(RedirectResult):
                                auditAction.ResponseBody = string.Format("Redirect to {0}", (context.Result as RedirectResult).Url);
                                break;
                            default:
                                // TODO: Handle other result types
                                auditAction.ResponseBody = string.Format("Result type: {0}", context.Result.GetType().Name);
                                break;
                        }
                    }
                }
                else
                {
                    auditAction.ResponseStatusCode = 500;
                    auditAction.ResponseStatus = "Internal Server Error";
                }
                // Replace the Action field and save
                (auditScope.Event as AuditEventWebApi).Action = auditAction;
                auditScope.Save();
            }
        }

        private static IDictionary<string, string> ToDictionary(IEnumerable<KeyValuePair<string, StringValues>> col)
        {
            if (col == null)
            {
                return null;
            }
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var k in col)
            {
                dict.Add(k.Key, string.Join(", ", k.Value));
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

        internal static AuditScope GetCurrentScope(HttpContext httpContext)
        {
            return httpContext.Items[AuditApiScopeKey] as AuditScope;
        }
    }
}
#endif