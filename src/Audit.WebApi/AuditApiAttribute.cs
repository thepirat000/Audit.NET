#if NET45
using System;
using Audit.Core;
using Audit.Core.Extensions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net;
using System.Linq;

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
        /// Gets or sets a value indicating whether the output should include the Http Response body
        /// (Use the alternative IncludeResponseBodyFor to log the body only for certain response status codes).
        /// </summary>
        public bool IncludeResponseBody { get; set; }
        /// <summary>
        /// Gets or sets an array of status codes that conditionally indicates when the response body should be included.
        /// The response body is included only when the request return any of these status codes.
        /// When set to NULL, the IncludeResponseBody boolean is used to determine whether to include the response body or not.
        /// When to set to non-NULL, the IncludeResponseBody boolean is ignored.
        /// </summary>
        public HttpStatusCode[] IncludeResponseBodyFor { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the output should include the Http request body string.
        /// </summary>
        public bool IncludeRequestBody { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the action arguments should be pre-serialized to the audit event.
        /// </summary>
        public bool SerializeActionParameters { get; set; }
        /// <summary>
        /// Gets or sets a string indicating the event type to use.
        /// Can contain the following placeholders:
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action method name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { get; set; }
        /// <summary>
        /// Gets or sets the class type that will be used as a context wrapper. 
        /// It must be a class implementing IContextWrapper with a public constructor receiving a single HttpRequestMessage parameter.
        /// Default is NULL to use the default ContextWrapper class.
        /// </summary>
        public Type ContextWrapperType { get; set; }


        private const string AuditApiActionKey = "__private_AuditApiAction__";
        private const string AuditApiScopeKey = "__private_AuditApiScope__";

        private IContextWrapper GetContextWrapper(HttpRequestMessage request)
        {
            if (ContextWrapperType == null)
            {
                return new ContextWrapper(request);
            }
            else
            {
                return Activator.CreateInstance(ContextWrapperType, new object[] { request }) as IContextWrapper;
            }
        }

        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        private async Task BeforeExecutingAsync(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var contextWrapper = GetContextWrapper(request);

            var auditAction = new AuditApiAction
            {
                UserName = actionContext.RequestContext?.Principal?.Identity?.Name,
                IpAddress = contextWrapper.GetClientIp(),
                RequestUrl = request.RequestUri?.AbsoluteUri,
                HttpMethod = actionContext.Request.Method?.Method,
                FormVariables = contextWrapper.GetFormVariables(),
                Headers = IncludeHeaders ? ToDictionary(request.Headers) : null,
                ActionName = actionContext.ActionDescriptor?.ActionName,
                ControllerName = actionContext.ActionDescriptor?.ControllerDescriptor?.ControllerName,
                ActionParameters = GetActionParameters(actionContext.ActionArguments),
                RequestBody = IncludeRequestBody ? GetRequestBody(contextWrapper) : null
            };
            var eventType = (EventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName);
            // Create the audit scope
            var auditEventAction = new AuditEventWebApi()
            {
                Action = auditAction
            };
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEventAction,
                CallingMethod = (actionContext.ActionDescriptor as ReflectedHttpActionDescriptor)?.MethodInfo
            };
            var auditScope = await AuditScope.CreateAsync(options);
            contextWrapper.Set(AuditApiActionKey, auditAction);
            contextWrapper.Set(AuditApiScopeKey, auditScope);
        }

        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        /// <param name="actionExecutedContext">The action executed context.</param>
        private async Task AfterExecutedAsync(HttpActionExecutedContext actionExecutedContext)
        {
            var contextWrapper = GetContextWrapper(actionExecutedContext.Request);
            var auditAction = contextWrapper.Get<AuditApiAction>(AuditApiActionKey);
            var auditScope = contextWrapper.Get<AuditScope>(AuditApiScopeKey);
            if (auditAction != null && auditScope != null)
            {
                auditAction.Exception = actionExecutedContext.Exception.GetExceptionInfo();
                auditAction.ModelStateErrors = IncludeModelState ? AuditApiHelper.GetModelStateErrors(actionExecutedContext.ActionContext.ModelState) : null;
                auditAction.ModelStateValid = IncludeModelState ? actionExecutedContext.ActionContext.ModelState?.IsValid : null;
                if (actionExecutedContext.Response != null)
                {
                    auditAction.ResponseStatus = actionExecutedContext.Response.ReasonPhrase;
                    auditAction.ResponseStatusCode = (int)actionExecutedContext.Response.StatusCode;
                    if (ShouldIncludeResponseBody(actionExecutedContext.Response.StatusCode))
                    {
                        var objContent = actionExecutedContext.Response.Content as ObjectContent;
                        auditAction.ResponseBody = new BodyContent
                        {
                            Type = objContent != null ? objContent.ObjectType.Name : actionExecutedContext.Response.Content?.Headers?.ContentType.ToString(),
                            Length = actionExecutedContext.Response.Content?.Headers.ContentLength,
                            Value = objContent != null ? objContent.Value : actionExecutedContext.Response.Content?.ReadAsStringAsync().Result
                        };
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

        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (Configuration.AuditDisabled)
            {
                return;
            }
            await BeforeExecutingAsync(actionContext);
        }

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (Configuration.AuditDisabled)
            {
                return;
            }
            await AfterExecutedAsync(actionExecutedContext);
        }

        private bool ShouldIncludeResponseBody(HttpStatusCode responseStatusCode)
        {
            if (IncludeResponseBodyFor != null)
            {
                return IncludeResponseBodyFor.Contains(responseStatusCode);
            }
            return IncludeResponseBody;
        }

        protected virtual BodyContent GetRequestBody(IContextWrapper contextWrapper)
        {
            var context = contextWrapper.GetHttpContext();
            if (context?.Request?.InputStream != null)
            {
                using (var stream = new MemoryStream())
                {
                    context.Request.InputStream.Seek(0, SeekOrigin.Begin);
                    context.Request.InputStream.CopyTo(stream);
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

        private IDictionary<string, object> GetActionParameters(IDictionary<string, object> actionArguments)
        {
            if (SerializeActionParameters)
            {
                return AuditApiHelper.SerializeParameters(actionArguments);
            }
            return actionArguments;
        }

        private static IDictionary<string, string> ToDictionary(HttpRequestHeaders col)
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
        
        internal static AuditScope GetCurrentScope(HttpRequestMessage request, IContextWrapper contextWrapper)
        {
            var ctx = contextWrapper ?? new ContextWrapper(request);
            return ctx.Get<AuditScope>(AuditApiScopeKey);
        }
    }
}
#endif