#if NET45
using System;
using Audit.Core;
using System.Threading.Tasks;
using Audit.Core.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http.Headers;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;

namespace Audit.WebApi
{
    internal class AuditApiAdapter
    {
        public bool IsActionIgnored(HttpActionContext actionContext)
        {
            var actionDescriptor = actionContext?.ActionDescriptor;
            var controllerIgnored = actionDescriptor?.ControllerDescriptor?.GetCustomAttributes<AuditIgnoreAttribute>(true).Any();
            if (controllerIgnored.GetValueOrDefault())
            {
                return true;
            }
            var actionIgnored = actionDescriptor?.GetCustomAttributes<AuditIgnoreAttribute>(true).Any();
            if (actionIgnored.GetValueOrDefault())
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        public async Task BeforeExecutingAsync(HttpActionContext actionContext, IContextWrapper contextWrapper, bool includeHeaders, bool includeRequestBody, bool serializeParams, string eventTypeName)
        {
            var request = actionContext.Request;
            
            var auditAction = new AuditApiAction
            {
                UserName = actionContext.RequestContext?.Principal?.Identity?.Name,
                IpAddress = contextWrapper.GetClientIp(),
                RequestUrl = request.RequestUri?.AbsoluteUri,
                HttpMethod = actionContext.Request.Method?.Method,
                FormVariables = contextWrapper.GetFormVariables(),
                Headers = includeHeaders ? ToDictionary(request.Headers) : null,
                ActionName = actionContext.ActionDescriptor?.ActionName,
                ControllerName = actionContext.ActionDescriptor?.ControllerDescriptor?.ControllerName,
                ActionParameters = GetActionParameters(actionContext.ActionDescriptor, actionContext.ActionArguments, serializeParams),
                RequestBody = includeRequestBody ? GetRequestBody(contextWrapper) : null,
                TraceId = request.GetCorrelationId().ToString()
            };
            var eventType = (eventTypeName ?? "{verb} {controller}/{action}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{controller}", auditAction.ControllerName)
                .Replace("{action}", auditAction.ActionName)
                .Replace("{url}", auditAction.RequestUrl);
            // Create the audit scope
            var auditEventAction = new AuditEventWebApi()
            {
                Action = auditAction
            };
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEventAction,
                // the inner ActionDescriptor is of type ReflectedHttpActionDescriptor even when using api versioning:
                CallingMethod = (actionContext.ActionDescriptor?.ActionBinding?.ActionDescriptor as ReflectedHttpActionDescriptor)?.MethodInfo
            };
            var auditScope = await AuditScope.CreateAsync(options);
            contextWrapper.Set(AuditApiHelper.AuditApiActionKey, auditAction);
            contextWrapper.Set(AuditApiHelper.AuditApiScopeKey, auditScope);
        }

        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        public async Task AfterExecutedAsync(HttpActionExecutedContext actionExecutedContext, IContextWrapper contextWrapper, bool includeModelState, bool includeResponseBody)
        {
            var auditAction = contextWrapper.Get<AuditApiAction>(AuditApiHelper.AuditApiActionKey);
            var auditScope = contextWrapper.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey);
            if (auditAction != null && auditScope != null)
            {
                auditAction.Exception = actionExecutedContext.Exception.GetExceptionInfo();
                auditAction.ModelStateErrors = includeModelState ? AuditApiHelper.GetModelStateErrors(actionExecutedContext.ActionContext.ModelState) : null;
                auditAction.ModelStateValid = includeModelState ? actionExecutedContext.ActionContext.ModelState?.IsValid : null;
                if (actionExecutedContext.Response != null)
                {
                    auditAction.ResponseStatus = actionExecutedContext.Response.ReasonPhrase;
                    auditAction.ResponseStatusCode = (int)actionExecutedContext.Response.StatusCode;
                    if (includeResponseBody)
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

        private IDictionary<string, object> GetActionParameters(HttpActionDescriptor actionDescriptor, IDictionary<string, object> actionArguments, bool serializeParams)
        {
            var args = actionArguments.ToDictionary(k => k.Key, v => v.Value);
            var parameters = actionDescriptor.GetParameters();
            if (parameters != null)
            {
                foreach(var param in parameters)
                {
                    if (param.GetCustomAttributes<AuditIgnoreAttribute>().Any())
                    {
                        args.Remove(param.ParameterName);
                    }
                }
            }
            if (serializeParams)
            {
                return AuditApiHelper.SerializeParameters(args);
            }
            return args;
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
            return ctx.Get<AuditScope>(AuditApiHelper.AuditApiScopeKey);
        }
    }
}
#endif