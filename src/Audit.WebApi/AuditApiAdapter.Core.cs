#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using System;
using Audit.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Audit.Core.Extensions;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Reflection;
using Microsoft.AspNetCore.Http.Extensions;

namespace Audit.WebApi
{
    internal class AuditApiAdapter
    {
        public bool IsActionIgnored(ActionExecutingContext actionContext)
        {
            var actionDescriptor = actionContext?.ActionDescriptor as ControllerActionDescriptor;
            var controllerIgnored = actionDescriptor?.MethodInfo?.DeclaringType.GetTypeInfo().GetCustomAttribute<AuditIgnoreAttribute>(true);
            if (controllerIgnored != null)
            {
                return true;
            }
            var actionIgnored = actionDescriptor?.MethodInfo?.GetCustomAttribute<AuditIgnoreAttribute>(true);
            if (actionIgnored != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        public async Task BeforeExecutingAsync(ActionExecutingContext actionContext,
            bool includeHeaders, bool includeRequestBody, bool serializeParams, string eventTypeName)
        {
            var httpContext = actionContext.HttpContext;
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;

            var auditAction = CreateOrUpdateAction(actionContext, includeHeaders, includeRequestBody, serializeParams, eventTypeName);

            var eventType = (eventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName)
                .Replace("{url}", auditAction.RequestUrl);
            // Create the audit scope
            var auditEventAction = new AuditEventWebApi()
            {
                Action = auditAction
            };
            var auditScope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = eventType, AuditEvent = auditEventAction, CallingMethod = actionDescriptor.MethodInfo });
            httpContext.Items[AuditApiHelper.AuditApiActionKey] = auditAction;
            httpContext.Items[AuditApiHelper.AuditApiScopeKey] = auditScope;
        }

        private AuditApiAction CreateOrUpdateAction(ActionExecutingContext actionContext,
            bool includeHeaders, bool includeRequestBody, bool serializeParams, string eventTypeName)
        {
            var httpContext = actionContext.HttpContext;
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            AuditApiAction action = null;
            if (httpContext.Items.ContainsKey(AuditApiHelper.AuditApiActionKey))
            {
                action = httpContext.Items[AuditApiHelper.AuditApiActionKey] as AuditApiAction;
            }
            if (action == null)
            {
                action = new AuditApiAction
                {
                    UserName = httpContext.User?.Identity.Name,
                    IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString(),
                    HttpMethod = httpContext.Request.Method,
                    FormVariables = AuditApiHelper.GetFormVariables(httpContext),
                    TraceId = httpContext.TraceIdentifier
                };
            }
            action.RequestUrl = httpContext.Request.GetDisplayUrl();
            action.ActionName = actionDescriptor != null ? actionDescriptor.ActionName : actionContext.ActionDescriptor.DisplayName;
            action.ControllerName = actionDescriptor?.ControllerName;
            action.ActionParameters = GetActionParameters(actionDescriptor, actionContext.ActionArguments, serializeParams);
            if (includeHeaders)
            {
                action.Headers = AuditApiHelper.ToDictionary(httpContext.Request.Headers);
            }
            if (includeRequestBody && action.RequestBody == null)
            {
                action.RequestBody = new BodyContent
                {
                    Type = httpContext.Request.ContentType,
                    Length = httpContext.Request.ContentLength,
                    Value = AuditApiHelper.GetRequestBody(httpContext)
                };
            }
            return action;
        }


        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        public async Task AfterExecutedAsync(ActionExecutedContext context, bool includeModelState, bool includeResponseBody)
        {
            var httpContext = context.HttpContext;
            var auditAction = httpContext.Items[AuditApiHelper.AuditApiActionKey] as AuditApiAction;
            var auditScope = httpContext.Items[AuditApiHelper.AuditApiScopeKey] as AuditScope;
            if (auditAction != null && auditScope != null)
            {
                auditAction.Exception = context.Exception.GetExceptionInfo();
                auditAction.ModelStateErrors = includeModelState ? AuditApiHelper.GetModelStateErrors(context.ModelState) : null;
                auditAction.ModelStateValid = includeModelState ? context.ModelState?.IsValid : null;
                if (context.HttpContext.Response != null && context.Result != null)
                {
                    var statusCode = context.Result is ObjectResult && (context.Result as ObjectResult).StatusCode.HasValue ? (context.Result as ObjectResult).StatusCode.Value
                        : context.Result is StatusCodeResult ? (context.Result as StatusCodeResult).StatusCode : context.HttpContext.Response.StatusCode;
                    auditAction.ResponseStatusCode = statusCode;
                    auditAction.ResponseStatus = AuditApiHelper.GetStatusCodeString(auditAction.ResponseStatusCode);
                    if (includeResponseBody)
                    {
                        var bodyType = context.Result.GetType().GetFullTypeName();
                        auditAction.ResponseBody = new BodyContent { Type = bodyType, Value = GetResponseBody(context.Result) };
                    }
                }
                else
                {
                    auditAction.ResponseStatusCode = 500;
                    auditAction.ResponseStatus = "Internal Server Error";
                }
                // Replace the Action field 
                (auditScope.Event as AuditEventWebApi).Action = auditAction;
                // Save, if action was not created by middleware
                if (!auditAction.IsMiddleware)
                {
                    await auditScope.SaveAsync();
                }
            }
        }

        private object GetResponseBody(IActionResult result)
        {
            if (result is ObjectResult or)
            {
                return or.Value;
            }
            if (result is StatusCodeResult sr)
            {
                return sr.StatusCode;
            }
            if (result is JsonResult jr)
            {
                return jr.Value;
            }
            if (result is ContentResult cr)
            {
                return cr.Content;
            }
            if (result is FileResult fr)
            {
                return fr.FileDownloadName;
            }
            if (result is LocalRedirectResult lrr)
            {
                return lrr.Url;
            }
            if (result is RedirectResult rr)
            {
                return rr.Url;
            }
            if (result is RedirectToActionResult rta)
            {
                return rta.ActionName;
            }
            if (result is RedirectToRouteResult rtr)
            {
                return rtr.RouteName;
            }
            if (result is SignInResult sir)
            {
                return sir.Principal?.Identity?.Name;
            }
            if (result is PartialViewResult pvr)
            {
                return pvr.ViewName;
            }
            if (result is ViewComponentResult vc)
            {
                return vc.ViewComponentName;
            }
            if (result is ViewResult vr)
            {
                return vr.ViewName;
            }
#if NETSTANDARD2_0
            if (result is RedirectToPageResult rtp)
            {
                return rtp.PageName;
            }
#endif
            return result.ToString();
        }

        private IDictionary<string, object> GetActionParameters(ControllerActionDescriptor actionDescriptor, IDictionary<string, object> actionArguments, bool serializeParams)
        {
            var args = actionArguments.ToDictionary(k => k.Key, v => v.Value); 
            foreach (var param in actionDescriptor.Parameters)
            {
                if ((param as ControllerParameterDescriptor)?.ParameterInfo.GetCustomAttribute<AuditIgnoreAttribute>(true) != null)
                {
                    args.Remove(param.Name);
                }
            }
            if (serializeParams)
            {
                return AuditApiHelper.SerializeParameters(args);
            }
            return args;
        }

        internal static AuditScope GetCurrentScope(HttpContext httpContext)
        {
            return httpContext.Items[AuditApiHelper.AuditApiScopeKey] as AuditScope;
        }

    }
}
#endif