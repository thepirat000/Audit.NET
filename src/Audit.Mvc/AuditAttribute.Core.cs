#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Audit.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Audit.Core.Extensions;
using System.IO;
using System.Text;

namespace Audit.Mvc
{
    /// <summary>
    /// Action Filter to Audit an Mvc Action
    /// </summary>
    public class AuditAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the serialized model.
        /// </summary>
        public bool IncludeModel { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the Request Body content should be read and incuded.
        /// </summary>
        /// <remarks>
        /// When IncludeResquestBody is set to true and you are not using a [FromBody] parameter (i.e.reading the request body directly from the Request)
        /// make sure you enable rewind on the request body stream, otherwise the controller won't be able to read the request body since, by default, 
        /// it's a forwand-only stream that can be read only once. 
        /// </remarks>
        public bool IncludeRequestBody { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Request Headers.
        /// </summary>
        public bool IncludeHeaders { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http Response body content.
        /// </summary>
        public bool IncludeResponseBody { get; set; }
        /// <summary>
        /// Gets or sets a value indicating the event type name
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

        private const string AuditActionKey = "__private_AuditAction__";
        private const string AuditScopeKey = "__private_AuditScope__";

        private async Task BeforeExecutingAsync(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var request = httpContext.Request;
            var actionDescriptior = filterContext.ActionDescriptor as ControllerActionDescriptor;

            var auditAction = new AuditAction()
            {
                UserName = httpContext.User?.Identity.Name,
                IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString(),
                RequestUrl = string.Format("{0}://{1}{2}", httpContext.Request.Scheme, httpContext.Request.Host, httpContext.Request.Path),
                HttpMethod = httpContext.Request.Method,
                FormVariables = httpContext.Request.HasFormContentType ? ToDictionary(httpContext.Request.Form) : null,
                Headers = IncludeHeaders ? ToDictionary(httpContext.Request.Headers) : null,
                ActionName = actionDescriptior?.ActionName ?? actionDescriptior?.DisplayName,
                ControllerName = actionDescriptior?.ControllerName,
                ActionParameters = GetActionParameters(filterContext.ActionArguments),
                RequestBody = new BodyContent { Type = httpContext.Request.ContentType, Length = httpContext.Request.ContentLength, Value = IncludeRequestBody ? GetRequestBody(filterContext) : null },
                TraceId = httpContext.TraceIdentifier
            };

            var eventType = (EventTypeName ?? "{verb} {controller}/{action}")
                .Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditEventAction = new AuditEventMvcAction()
            {
                Action = auditAction
            };
            var auditScope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = eventType, AuditEvent = auditEventAction, CallingMethod = actionDescriptior?.MethodInfo });
            httpContext.Items[AuditActionKey] = auditAction;
            httpContext.Items[AuditScopeKey] = auditScope;
        }

        private async Task AfterExecutedAsync(ActionExecutedContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var viewResult = filterContext.Result as ViewResult;
            var auditAction = httpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                auditAction.ModelStateErrors = IncludeModel ? GetModelStateErrors(filterContext.ModelState) : null;
                auditAction.Model = IncludeModel ? viewResult?.ViewData.Model : null;
                auditAction.ModelStateValid = IncludeModel ? viewResult?.ViewData.ModelState?.IsValid : null;
                auditAction.Exception = filterContext.Exception.GetExceptionInfo();

                var bodyType = filterContext.Result?.GetType().GetFullTypeName();
                if (bodyType != null)
                {
                    auditAction.ResponseBody = new BodyContent { Type = bodyType, Value = IncludeResponseBody ? GetResponseBody(filterContext.Result) : null };
                }
            }

            var auditScope = httpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field
                (auditScope.Event as AuditEventMvcAction).Action = auditAction;
            }
            if (auditAction?.Exception != null)
            {
                // An exception was thrown, save the event since OnResultExecutionAsync will not be triggered.
                await auditScope.SaveAsync();
            }
        }


        public async Task AfterResultAsync(ResultExecutedContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var auditAction = httpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                var viewResult = filterContext.Result as ViewResult;
                auditAction.ViewName = viewResult?.ViewName ?? auditAction.ActionName;
                auditAction.RedirectLocation = httpContext.Response.Headers?["Location"];
                auditAction.ResponseStatus = httpContext.Response.StatusCode.ToString();
                auditAction.ResponseStatusCode = httpContext.Response.StatusCode;
                auditAction.Exception = filterContext.Exception.GetExceptionInfo();
                var bodyType = filterContext.Result?.GetType().GetFullTypeName();
                if (bodyType != null)
                {
                    auditAction.ResponseBody = new BodyContent { Type = bodyType, Value = IncludeResponseBody ? GetResponseBody(filterContext.Result) : null };
                }
            }
            var auditScope = httpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field
                (auditScope.Event as AuditEventMvcAction).Action = auditAction;
                if (auditScope.EventCreationPolicy == EventCreationPolicy.Manual)
                {
                    await auditScope.SaveAsync();
                }
                await auditScope.DisposeAsync();
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (Configuration.AuditDisabled)
            {
                await next.Invoke();
                return;
            }
            await BeforeExecutingAsync(context);
            var actionExecutedContext = await next.Invoke();
            await AfterExecutedAsync(actionExecutedContext);
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (Configuration.AuditDisabled)
            {
                await next.Invoke();
                return;
            }
            var resultExecutionContext = await next.Invoke();
            await AfterResultAsync(resultExecutionContext);
        }

        private IDictionary<string, object> GetActionParameters(IDictionary<string, object> actionArguments)
        {
            if (SerializeActionParameters)
            {
                return AuditHelper.SerializeParameters(actionArguments);
            }
            return actionArguments.ToDictionary(k => k.Key, v => v.Value);
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

        internal static AuditScope GetCurrentScope(HttpContext httpContext)
        {
            return httpContext?.Items[AuditScopeKey] as AuditScope;
        }
    }


}
#endif