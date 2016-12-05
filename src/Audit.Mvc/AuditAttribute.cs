#if NET45
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Audit.Core;
using Audit.Core.Extensions;

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
        /// Gets or sets a value indicating whether the output should include the Http Request Headers.
        /// </summary>
        public bool IncludeHeaders { get; set; }
        /// <summary>
        /// Gets or sets a value indicating the event type name
        /// Can contain the following placeholders:
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action method name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { get; set; }

        private const string AuditActionKey = "__private_AuditAction__";
        private const string AuditScopeKey = "__private_AuditScope__";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var auditAction = new AuditAction()
            {
                UserName = (request.IsAuthenticated) ? filterContext.HttpContext.User?.Identity.Name : "Anonymous",
                IpAddress = request.ServerVariables?["HTTP_X_FORWARDED_FOR"] ?? request.UserHostAddress,
                RequestUrl = request.RawUrl,
                HttpMethod = request.HttpMethod,
                FormVariables = ToDictionary(request.Form),
                Headers = IncludeHeaders ? ToDictionary(request.Headers) : null,
                ActionName = filterContext.ActionDescriptor?.ActionName,
                ControllerName = filterContext.ActionDescriptor?.ControllerDescriptor?.ControllerName,
                ActionParameters = filterContext.ActionParameters?.ToDictionary(k => k.Key, v => v.Value)
            };
            var eventType = (EventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditEventAction = new AuditEventMvcAction()
            {
                Action = auditAction
            };
            var auditScope = AuditScope.Create(eventType, null, null, Configuration.CreationPolicy, null, auditEventAction);
            filterContext.HttpContext.Items[AuditActionKey] = auditAction;
            filterContext.HttpContext.Items[AuditScopeKey] = auditScope;
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var auditAction = filterContext.HttpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                auditAction.ModelStateErrors = IncludeModel ? GetModelStateErrors(filterContext.Controller?.ViewData.ModelState) : null;
                auditAction.Model = IncludeModel ? filterContext.Controller?.ViewData.Model : null;
                auditAction.ModelStateValid = IncludeModel ? filterContext.Controller?.ViewData.ModelState.IsValid : null;
                auditAction.RedirectLocation = filterContext.HttpContext.Response.RedirectLocation;
                auditAction.ResponseStatus = filterContext.HttpContext.Response.Status;
                auditAction.ResponseStatusCode = filterContext.HttpContext.Response.StatusCode;
                auditAction.Exception = filterContext.Exception.GetExceptionInfo();
            }
            var auditScope = filterContext.HttpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field and save
                (auditScope.Event as AuditEventMvcAction).Action = auditAction;
                auditScope.Save();
            }
            base.OnActionExecuted(filterContext);
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            var auditAction = filterContext.HttpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                var viewResult = filterContext.Result as ViewResult;
                var razorView = viewResult?.View as RazorView;
                auditAction.ViewName = viewResult?.ViewName;
                auditAction.ViewPath = razorView?.ViewPath;
                auditAction.Exception = filterContext.Exception.GetExceptionInfo();
            }
            var auditScope = filterContext.HttpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field and save
                (auditScope.Event as AuditEventMvcAction).Action = auditAction;
                auditScope.Save();
                auditScope.Dispose();
            }
            base.OnResultExecuted(filterContext);
        }

        private static IDictionary<string, string> ToDictionary(NameValueCollection col)
        {
            if (col == null)
            {
                return null;
            }
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

        internal static AuditScope GetCurrentScope(HttpContextBase httpContext)
        {
            return httpContext?.Items[AuditScopeKey] as AuditScope;
        }
    }
}
#endif