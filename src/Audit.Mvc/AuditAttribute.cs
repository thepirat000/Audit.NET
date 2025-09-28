#if ASP_NET
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <summary>
        /// Gets or sets a value indicating whether the child actions should be audited.
        /// </summary>
        public bool IncludeChildActions { get; set; }

        private const string AuditActionKey = "__private_AuditAction__";
        private const string AuditScopeKey = "__private_AuditScope__";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (IsActionIgnored(filterContext.ActionDescriptor, filterContext.IsChildAction))
            {
                base.OnActionExecuting(filterContext);
                return;
            }
            var request = filterContext.HttpContext.Request;
            var auditAction = new AuditAction()
            {
                UserName = (request.IsAuthenticated) ? filterContext.HttpContext.User?.Identity.Name : "Anonymous",
                IpAddress = request.ServerVariables?["HTTP_X_FORWARDED_FOR"] ?? request.UserHostAddress,
                RequestUrl = request.RawUrl,
                FormVariables = ToDictionary(request.Form),
                Headers = IncludeHeaders ? ToDictionary(request.Headers) : null,
                RequestBody = IncludeRequestBody ? GetRequestBody(filterContext.HttpContext) : null,
                HttpMethod = request.HttpMethod,
                ActionName = filterContext.ActionDescriptor?.ActionName,
                ControllerName = filterContext.ActionDescriptor?.ControllerDescriptor?.ControllerName,
                ActionParameters = GetActionParameters(filterContext),
                TraceId = null
            };
            var eventType = (EventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditEventAction = new AuditEventMvcAction()
            {
                Action = auditAction
            };
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEventAction,
                CallingMethod = (filterContext.ActionDescriptor as ReflectedActionDescriptor)?.MethodInfo
            };
            var auditScope = Configuration.AuditScopeFactory.Create(options);
            filterContext.HttpContext.Items[AuditActionKey] = auditAction;
            filterContext.HttpContext.Items[AuditScopeKey] = auditScope;
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (IsActionIgnored(filterContext.ActionDescriptor, filterContext.IsChildAction))
            {
                base.OnActionExecuted(filterContext);
                return;
            }
            var auditAction = filterContext.HttpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                auditAction.ModelStateErrors = IncludeModel ? AuditHelper.GetModelStateErrors(filterContext.Controller?.ViewData.ModelState) : null;
                auditAction.Model = IncludeModel ? filterContext.Controller?.ViewData.Model : null;
                auditAction.ModelStateValid = IncludeModel ? filterContext.Controller?.ViewData.ModelState.IsValid : null;
                auditAction.Exception = filterContext.Exception.GetExceptionInfo();
                auditAction.ResponseBody = IncludeResponseBody ? GetResponseBody(filterContext.Result) : null;
            }

            if (filterContext.HttpContext.Items[AuditScopeKey] is AuditScope auditScope)
            {
                // Replace the Action field
                auditScope.EventAs<AuditEventMvcAction>().Action = auditAction;
                if (auditAction?.Exception != null)
                {
                    // An exception was thrown, save the event since OnResultExecuted will not be triggered.
                    auditScope.Save();
                }
            }
            base.OnActionExecuted(filterContext);
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (IsActionIgnored(null, filterContext.IsChildAction))
            {
                base.OnResultExecuted(filterContext);
                return;
            }
            var auditAction = filterContext.HttpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                var viewResult = filterContext.Result as ViewResult;
                var razorView = viewResult?.View as RazorView;
                auditAction.ViewName = viewResult?.ViewName;
                auditAction.ViewPath = razorView?.ViewPath;
                auditAction.RedirectLocation = filterContext.HttpContext.Response.RedirectLocation;
                auditAction.ResponseStatus = filterContext.HttpContext.Response.Status;
                auditAction.ResponseStatusCode = filterContext.HttpContext.Response.StatusCode;
                auditAction.Exception = filterContext.Exception.GetExceptionInfo();
                auditAction.ResponseBody = IncludeResponseBody ? GetResponseBody(filterContext.Result) : null;
            }

            if (filterContext.HttpContext.Items[AuditScopeKey] is AuditScope auditScope)
            {
                // Replace the Action field 
                auditScope.EventAs<AuditEventMvcAction>().Action = auditAction;
                if (auditScope.EventCreationPolicy == EventCreationPolicy.Manual)
                {
                    auditScope.Save(); // for backwards compatibility
                }
                auditScope.Dispose();
            }
            base.OnResultExecuted(filterContext);
        }

        internal bool IsActionIgnored(ActionDescriptor actionDescriptor, bool isChildAction)
        {
            if (Configuration.AuditDisabled || !IncludeChildActions && isChildAction)
            {
                return true;
            }

            if (actionDescriptor == null)
            {
                return false;
            }

            var controllerIgnored = actionDescriptor.ControllerDescriptor.ControllerType
                .GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Any();
            if (controllerIgnored)
            {
                return true;
            }

            return actionDescriptor.GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Any();
        }

        internal IDictionary<string, object> GetActionParameters(ActionExecutingContext context)
        {
            var actionArguments = context.ActionDescriptor.GetParameters()
                .Where(pd => context.ActionParameters.ContainsKey(pd.ParameterName)
                             && !pd.GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Any())
                .ToDictionary(k => k.ParameterName, v => context.ActionParameters[v.ParameterName]);
            if (SerializeActionParameters)
            {
                return AuditHelper.SerializeParameters(actionArguments);
            }
            return actionArguments;
        }

        internal static IDictionary<string, string> ToDictionary(NameValueCollection col)
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

        protected virtual BodyContent GetRequestBody(HttpContextBase context)
        {
            if (context?.Request?.InputStream != null)
            {
                using (var stream = new MemoryStream())
                {
                    context.Request.InputStream.Seek(0, SeekOrigin.Begin);
                    context.Request.InputStream.CopyToAsync(stream).GetAwaiter().GetResult();
                    var body = Encoding.UTF8.GetString(stream.ToArray());
                    return new BodyContent
                    {
                        Type = context.Request.ContentType,
                        Length = context.Request.ContentLength,
                        Value = body
                    };
                }
            }
            return null;
        }

        internal BodyContent GetResponseBody(ActionResult result)
        {
            var content = new BodyContent() { Type = result.GetType().Name };
            if (result is ContentResult cr)
            {
                content.Length = cr.Content?.Length;
                content.Type = cr.ContentType;
                content.Value = cr.Content;
            }
            else if (result is EmptyResult er)
            {
                content.Value = "";
            }
            else if (result is FileResult fr)
            {
                content.Value = fr.FileDownloadName;
            }
            else if (result is JsonResult jr)
            {
                content.Value = jr.Data;
            }
            else if (result is JavaScriptResult jsr)
            {
                content.Value = jsr.Script;
            }
            else if (result is RedirectResult rr)
            {
                content.Value = rr.Url;
            }
            else if (result is RedirectToRouteResult rtr)
            {
                content.Value = rtr.RouteName;
            }
            else if (result is PartialViewResult pvr)
            {
                content.Value = pvr.ViewName;
            }
            else if (result is ViewResultBase vr)
            {
                content.Value = vr.ViewName;
            }
            else
            {
                content.Value = result.ToString();
            }
            return content;
        }

        internal static AuditScope GetCurrentScope(HttpContextBase httpContext)
        {
            return httpContext?.Items[AuditScopeKey] as AuditScope;
        }
    }
}
#endif