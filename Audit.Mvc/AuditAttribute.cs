using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using Audit.Core;

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
        /// Gets or sets a value indicating the event type
        /// </summary>
        public string EventType { get; set; }

        private const string AuditActionKey = "__private_AuditAction__";
        private const string AuditScopeKey = "__private_AuditScope__";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var auditAction = new AuditAction()
            {
                UserName = (request.IsAuthenticated) ? filterContext.HttpContext.User.Identity.Name : "Anonymous",
                IpAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? request.UserHostAddress,
                RequestUrl = request.RawUrl,
                HttpMethod = request.HttpMethod,
                FormVariables = ToDictionary(request.Form),
                Headers = IncludeHeaders ? ToDictionary(request.Headers) : null,
                ActionName = filterContext.ActionDescriptor.ActionName,
                ControllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName,
                ActionParameters = filterContext.ActionParameters.ToDictionary(k => k.Key, v => v.Value)
            };
            // Create the audit scope
            var auditScope = new AuditScope(EventType ?? $"{auditAction.ControllerName}/{auditAction.ActionName} ({auditAction.HttpMethod})");
            auditScope.SetCustomField("Action", auditAction);
            filterContext.HttpContext.Items[AuditActionKey] = auditAction;
            filterContext.HttpContext.Items[AuditScopeKey] = auditScope;
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var auditAction = filterContext.HttpContext.Items[AuditActionKey] as AuditAction;
            if (auditAction != null)
            {
                auditAction.ModelStateErrors = IncludeModel ? GetModelStateErrors(filterContext.Controller.ViewData.ModelState) : null;
                auditAction.Model = IncludeModel ? filterContext.Controller.ViewData.Model : null;
                auditAction.ModelStateValid = IncludeModel ? filterContext.Controller.ViewData.ModelState.IsValid : (bool?)null;
                auditAction.RedirectLocation = filterContext.HttpContext.Response.RedirectLocation;
                auditAction.ResponseStatus = filterContext.HttpContext.Response.Status;
                auditAction.ResponseStatusCode = filterContext.HttpContext.Response.StatusCode;
            }
            var auditScope = filterContext.HttpContext.Items[AuditScopeKey] as AuditScope;
            if (auditScope != null)
            {
                // Replace the Action field
                auditScope.SetCustomField("Action", auditAction);
                auditScope.Save();
                auditScope.Dispose();
            }
            base.OnActionExecuted(filterContext);
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
    }
}
