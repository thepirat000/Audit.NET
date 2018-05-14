#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
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

namespace Audit.WebApi
{
    internal class AuditApiAdapter
    {
        private const string AuditApiActionKey = "__private_AuditApiAction__";
        private const string AuditApiScopeKey = "__private_AuditApiScope__";
        
        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        public async Task BeforeExecutingAsync(ActionExecutingContext actionContext,
            bool includeHeaders, bool includeRequestBody, bool serializeParams, string eventTypeName)
        {
            var httpContext = actionContext.HttpContext;
            var actionDescriptior = actionContext.ActionDescriptor as ControllerActionDescriptor;
            var auditAction = new AuditApiAction
            {
                UserName = httpContext.User?.Identity.Name,
                IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString(),
                RequestUrl = string.Format("{0}://{1}{2}", httpContext.Request.Scheme, httpContext.Request.Host, httpContext.Request.Path),
                HttpMethod = actionContext.HttpContext.Request.Method,
                FormVariables = httpContext.Request.HasFormContentType ? ToDictionary(httpContext.Request.Form) : null,
                Headers = includeHeaders ? ToDictionary(httpContext.Request.Headers) : null,
                ActionName = actionDescriptior != null ? actionDescriptior.ActionName : actionContext.ActionDescriptor.DisplayName,
                ControllerName = actionDescriptior != null ? actionDescriptior.ControllerName : null,
                ActionParameters = GetActionParameters(actionContext.ActionArguments, serializeParams),
                RequestBody = new BodyContent { Type = httpContext.Request.ContentType, Length = httpContext.Request.ContentLength, Value = includeRequestBody ? GetRequestBody(actionContext) : null }
            };
            var eventType = (eventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditEventAction = new AuditEventWebApi()
            {
                Action = auditAction
            };
            var auditScope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = eventType, AuditEvent = auditEventAction, CallingMethod = actionDescriptior.MethodInfo });
            httpContext.Items[AuditApiActionKey] = auditAction;
            httpContext.Items[AuditApiScopeKey] = auditScope;
        }

        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        /// <param name="context">The action executed context.</param>
        public async Task AfterExecutedAsync(ActionExecutedContext context, bool includeModelState, bool includeResponseBody)
        {
            var httpContext = context.HttpContext;
            var auditAction = httpContext.Items[AuditApiActionKey] as AuditApiAction;
            var auditScope = httpContext.Items[AuditApiScopeKey] as AuditScope;
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
                    auditAction.ResponseStatus = GetStatusCodeString(auditAction.ResponseStatusCode);
                    if (includeResponseBody)
                    {
                        var bodyType = context.Result?.GetType().GetFullTypeName();
                        if (bodyType != null)
                        {
                            auditAction.ResponseBody = new BodyContent { Type = bodyType, Value = GetResponseBody(context.Result) };
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
                await auditScope.SaveAsync();
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

        private string GetStatusCodeString(int statusCode)
        {
            var name = ((HttpStatusCode)statusCode).ToString();
            string[] words = Regex.Matches(name, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();
            return words.Length == 0 ? name : string.Join(" ", words);
        }

        private IDictionary<string, object> GetActionParameters(IDictionary<string, object> actionArguments, bool serializeParams)
        {
            if (serializeParams)
            {
                return AuditApiHelper.SerializeParameters(actionArguments);
            }
            return actionArguments;
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

        internal static AuditScope GetCurrentScope(HttpContext httpContext)
        {
            return httpContext.Items[AuditApiScopeKey] as AuditScope;
        }

        private string GetRequestBody(ActionExecutingContext actionContext)
        {
            var body = actionContext.HttpContext.Request.Body;
            if (body != null && body.CanRead)
            {
                using (var stream = new MemoryStream())
                {
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    body.CopyTo(stream);
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            return null;
        }

    }
}
#endif