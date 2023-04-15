#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451 || NET5_0
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
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Threading;

namespace Audit.Mvc
{
    /// <summary>
    /// Action Filter to Audit an Mvc Action
    /// </summary>
    public class AuditAttribute : ActionFilterAttribute
    {
        // Default stream copy buffer size (from System.IO::Stream)
        private const int DefaultCopyBufferSize = 81920;

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
            var actionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            var requestCancellationToken = httpContext.RequestAborted;

            var auditAction = new AuditAction()
            {
                UserName = httpContext.User?.Identity.Name,
                IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString(),
                RequestUrl = string.Format("{0}://{1}{2}", request.Scheme, request.Host, request.Path),
                HttpMethod = request.Method,
                FormVariables = request.HasFormContentType ? ToDictionary(request.Form) : null,
                Headers = IncludeHeaders ? ToDictionary(request.Headers) : null,
                ActionName = actionDescriptor?.ActionName ?? actionDescriptor?.DisplayName,
                ControllerName = actionDescriptor?.ControllerName,
                ActionParameters = GetActionParameters(filterContext),
                RequestBody = new BodyContent
                {
                    Type = request.ContentType, 
                    Length = request.ContentLength, 
                    Value = IncludeRequestBody ? await GetRequestBody(filterContext, requestCancellationToken) : null
                },
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
            var auditScopeOptions = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEventAction,
                CallingMethod = actionDescriptor?.MethodInfo
            };
            var auditScope = await AuditScope.CreateAsync(auditScopeOptions, requestCancellationToken);
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
                    auditAction.ResponseBody = new BodyContent
                    {
                        Type = bodyType, 
                        Value = IncludeResponseBody ? GetResponseBody(filterContext.ActionDescriptor, filterContext.Result) : null
                    };
                }
            }

            var auditScope = httpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field
                ((AuditEventMvcAction)auditScope.Event).Action = auditAction;
                if (auditAction?.Exception != null)
                {
                    // An exception was thrown, save the event since OnResultExecutionAsync will not be triggered.
                    await auditScope.SaveAsync();
                }
            }
        }

        private async Task AfterResultAsync(ResultExecutedContext filterContext)
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
                    auditAction.ResponseBody = new BodyContent
                    {
                        Type = bodyType, 
                        Value = IncludeResponseBody ? GetResponseBody(filterContext.ActionDescriptor, filterContext.Result) : null
                    };
                }
            }
            var auditScope = httpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field
                ((AuditEventMvcAction)auditScope.Event).Action = auditAction;
                if (auditScope.EventCreationPolicy == EventCreationPolicy.Manual)
                {
                    await auditScope.SaveAsync();
                }
                await auditScope.DisposeAsync();
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (IsActionIgnored(context.ActionDescriptor))
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
            if (IsActionIgnored(context.ActionDescriptor))
            {
                await next.Invoke();
                return;
            }
            var resultExecutionContext = await next.Invoke();
            await AfterResultAsync(resultExecutionContext);
        }

        private IDictionary<string, object> GetActionParameters(ActionExecutingContext context)
        {
            var actionArguments = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo.GetParameters()
                .Where(pi => context.ActionArguments.ContainsKey(pi.Name)
#if NETSTANDARD1_6
                && !pi.CustomAttributes.Any(ca => ca.AttributeType == typeof(AuditIgnoreAttribute)))
#else
                && !pi.GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Any())
#endif
                .ToDictionary(k => k.Name, v => context.ActionArguments[v.Name]);
            if (SerializeActionParameters)
            {
                return AuditHelper.SerializeParameters(actionArguments);
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

        private async Task<string> GetRequestBody(ActionExecutingContext actionContext, CancellationToken cancellationToken)
        {
            var body = actionContext.HttpContext.Request.Body;
            if (body != null && body.CanRead)
            {
                using (var stream = new MemoryStream())
                {
                    int bufferSize = DefaultCopyBufferSize;
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                        bufferSize = (int)Math.Min(bufferSize, body.Length < 2 ? 1 : body.Length);
                    }
                    await body.CopyToAsync(stream, bufferSize, cancellationToken);
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            return null;
        }

        private object GetResponseBody(ActionDescriptor descriptor, IActionResult result)
        {
            if ((descriptor as ControllerActionDescriptor)?.MethodInfo
                .ReturnTypeCustomAttributes
                .GetCustomAttributes(typeof(AuditIgnoreAttribute), true)
                .Any() == true)
            {
                return null;
            }
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
#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NET5_0
            if (result is RedirectToPageResult rtp)
            {
                return rtp.PageName;
            }
#endif
            return result.ToString();
        }

        private bool IsActionIgnored(ActionDescriptor actionDescriptor)
        {
            if (Configuration.AuditDisabled)
            {
                return true;
            }

            if (actionDescriptor == null)
            {
                return false;
            }

            var controllerIgnored = (actionDescriptor as ControllerActionDescriptor).ControllerTypeInfo
#if NETSTANDARD1_6
                .CustomAttributes.Any(ca => ca.AttributeType == typeof(AuditIgnoreAttribute));
#else
                .GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Any();
#endif
            if (controllerIgnored)
            {
                return true;
            }
            var methodIgnored = (actionDescriptor as ControllerActionDescriptor).MethodInfo
#if NETSTANDARD1_6
                .CustomAttributes.Any(ca => ca.AttributeType == typeof(AuditIgnoreAttribute));
#else
                .GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Any();
#endif
            return methodIgnored;
        }


        internal static AuditScope GetCurrentScope(HttpContext httpContext)
        {
            return httpContext?.Items[AuditScopeKey] as AuditScope;
        }
    }
}
#endif