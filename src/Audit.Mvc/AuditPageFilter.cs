﻿#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NET5_0
using Audit.Core;
using Audit.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Audit.Mvc
{
    public class AuditPageFilter : IAsyncPageFilter
    {
        private const string AuditActionKey = "__private_AuditAction__";
        private const string AuditScopeKey = "__private_AuditScope__";

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
        /// Gets or sets a value indicating the event type name. Default is "{verb} {path}".
        /// Can contain the following placeholders:
        /// - {path}: replaced with the complete view path.
        /// - {area}: replaced with the area name.
        /// - {action}: replaced with the action display name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { get; set; }

        #region IAsyncPageFilter implementation

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (Configuration.AuditDisabled || IsActionIgnored(context))
            {
                await next.Invoke();
                return;
            }
            await BeforeExecutingAsync(context);
            var actionExecutedContext = await next.Invoke();
            await AfterExecutedAsync(actionExecutedContext);
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        #endregion

        private bool IsActionIgnored(PageHandlerExecutingContext context)
        {
            return context.ActionDescriptor?.HandlerTypeInfo.GetCustomAttribute<AuditIgnoreAttribute>() != null
                || context.HandlerMethod.MethodInfo.GetCustomAttribute<AuditIgnoreAttribute>() != null;
        }

        public virtual async Task BeforeExecutingAsync(PageHandlerExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var actionDescriptior = context.ActionDescriptor;

            var auditAction = new AuditAction()
            {
                UserName = httpContext.User?.Identity.Name,
                IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString(),
                RequestUrl = string.Format("{0}://{1}{2}{3}", request.Scheme, request.Host, request.Path, request.QueryString),
                HttpMethod = request.Method,
                FormVariables = request.HasFormContentType ? AuditHelper.ToDictionary(request.Form) : null,
                Headers = IncludeHeaders ? AuditHelper.ToDictionary(request.Headers) : null,
                ActionName = actionDescriptior?.DisplayName,
                ControllerName = actionDescriptior?.AreaName,
                ActionParameters = GetActionParameters(context),
                RequestBody = new BodyContent { Type = request.ContentType, Length = request.ContentLength, Value = IncludeRequestBody ? await GetRequestBody(context) : null },
                TraceId = httpContext.TraceIdentifier,
                ViewPath = actionDescriptior?.ViewEnginePath,
                PageHandlerExecutingContext = context
            };

            var eventType = (EventTypeName ?? "{verb} {path}")
                .Replace("{verb}", auditAction.HttpMethod)
                .Replace("{area}", auditAction.ControllerName)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName)
                .Replace("{path}", auditAction.ViewPath);
            // Create the audit scope
            var auditEventAction = new AuditEventMvcAction()
            {
                Action = auditAction
            };
            var auditScope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = eventType, AuditEvent = auditEventAction, CallingMethod = context.HandlerMethod?.MethodInfo });
            httpContext.Items[AuditActionKey] = auditAction;
            httpContext.Items[AuditScopeKey] = auditScope;
        }

        public virtual async Task AfterExecutedAsync(PageHandlerExecutedContext context)
        {
            var httpContext = context.HttpContext;
            var auditAction = httpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                auditAction.ModelStateErrors = IncludeModel ? AuditHelper.GetModelStateErrors(context.ModelState) : null;
                auditAction.Model = IncludeModel ? GetModelObject(context) : null;
                auditAction.ModelStateValid = IncludeModel ? context.ModelState?.IsValid : null;
                auditAction.Exception = context.Exception.GetExceptionInfo();
                auditAction.RedirectLocation = httpContext.Response.Headers?["Location"];
                auditAction.ResponseStatusCode = context.Result == null && context.Exception != null && !context.ExceptionHandled
                    ? 500
                    : context.Result is StatusCodeResult
                        ? (context.Result as StatusCodeResult).StatusCode
                        : httpContext.Response.StatusCode;

                var bodyType = context.Result?.GetType().GetFullTypeName();
                var method = auditAction.PageHandlerExecutingContext?.HandlerMethod?.MethodInfo;
                if (bodyType != null)
                {
                    auditAction.ResponseBody = new BodyContent { Type = bodyType, Value = IncludeResponseBody ? GetResponseBody(method, context.Result) : null };
                }
            }

            var auditScope = httpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field
                (auditScope.Event as AuditEventMvcAction).Action = auditAction;
                // Save the event and dispose the scope
                await auditScope.DisposeAsync();
            }
        }

        private async Task<string> GetRequestBody(PageHandlerExecutingContext context)
        {
            var body = context.HttpContext.Request.Body;
            if (body != null && body.CanRead)
            {
                using (var stream = new MemoryStream())
                {
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    await body.CopyToAsync(stream);
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            return null;
        }

        private Dictionary<string, object> GetModelObject(PageHandlerExecutedContext context)
        {
            if (context.ActionDescriptor.BoundProperties == null || context.ActionDescriptor.BoundProperties.Count == 0)
            {
                return null;
            }
            var model = new Dictionary<string, object>();
            foreach (var prop in context.ActionDescriptor.BoundProperties)
            {
                if (prop is PageBoundPropertyDescriptor descriptor)
                {
                    model.Add(prop.Name, descriptor.Property.GetValue(context.HandlerInstance));
                }
            }
            return model;
        }

        private object GetResponseBody(MethodInfo method, IActionResult result)
        {
            if (method?.ReturnTypeCustomAttributes
                .GetCustomAttributes(typeof(AuditIgnoreAttribute), true)
                .Any() == true)
            {
                return null;
            }
            if (result is PageResult pr)
            {
                return pr.Page?.BodyContent;
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
            if (result is RedirectToPageResult rtp)
            {
                return rtp.PageName;
            }
            return result.ToString();
        }

        private IDictionary<string, object> GetActionParameters(PageHandlerExecutingContext context)
        {
            var actionArguments = context.HandlerArguments
                .Where(pi => context.HandlerMethod.Parameters?.FirstOrDefault(pp => pp.Name == pi.Key)?.ParameterInfo.GetCustomAttribute<AuditIgnoreAttribute>(true) == null)
                .ToDictionary(k => k.Key, v => v.Value);

            return actionArguments;
        }
    }
}
#endif