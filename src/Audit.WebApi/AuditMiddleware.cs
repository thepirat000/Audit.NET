#if ASP_CORE
using Audit.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http.Extensions;
using Audit.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.WebApi
{
    /// <summary>
    /// Middleware to audit requests
    /// </summary>
    public class AuditMiddleware
    {
        // Default stream copy buffer size (from System.IO::Stream)
        private const int DefaultCopyBufferSize = 81920;

        private readonly RequestDelegate _next;

        private readonly ConfigurationApi.AuditMiddlewareConfigurator _config;

        public AuditMiddleware(RequestDelegate next, ConfigurationApi.AuditMiddlewareConfigurator config)
        {
            _next = next;
            _config = config ?? new ConfigurationApi.AuditMiddlewareConfigurator();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (Configuration.AuditDisabled)
            {
                await _next.Invoke(context);
                return;
            }
            var includeHeaders = _config._includeRequestHeadersBuilder != null && _config._includeRequestHeadersBuilder.Invoke(context);
            var includeResponseHeaders = _config._includeResponseHeadersBuilder != null && _config._includeResponseHeadersBuilder.Invoke(context);
            var includeRequest = _config._includeRequestBodyBuilder != null && _config._includeRequestBodyBuilder.Invoke(context);
            var eventTypeName = _config._eventTypeNameBuilder?.Invoke(context);
            var includeResponse = _config._includeResponseBodyBuilder != null && _config._includeResponseBodyBuilder.Invoke(context);
            var originalBody = context.Response.Body;

            // pre-filter
            if (_config._requestFilter != null && !_config._requestFilter.Invoke(context.Request))
            {
                await _next.Invoke(context);
                return;
            }

            var requestCancellationToken = context.RequestAborted;

            await BeforeInvoke(context, includeHeaders, includeRequest, eventTypeName, requestCancellationToken);

            if (includeResponse)
            {
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;
                    await InvokeNextAsync(context, true, includeResponseHeaders, originalBody);
                    responseBody.Seek(0L, SeekOrigin.Begin);
                    int bufferSize = (int)Math.Min(DefaultCopyBufferSize, responseBody.Length < 2 ? 1 : responseBody.Length);
                    await responseBody.CopyToAsync(originalBody, bufferSize, requestCancellationToken);
                    context.Response.Body = originalBody;
                }
            }
            else
            {
                await InvokeNextAsync(context, false, includeResponseHeaders, originalBody);
            }
        }

        private async Task InvokeNextAsync(HttpContext context, bool includeResponseBody, bool includeResponseHeaders, Stream originalBody)
        {
            Exception exception = null;
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
#pragma warning disable IDE0059 // Value assigned to symbol is never used
                exception = ex;
#pragma warning restore IDE0059 // Value assigned to symbol is never used
                context.Response.Body = originalBody;
                throw;
            }
            finally
            {
                await AfterInvoke(context, includeResponseBody, includeResponseHeaders, exception);
            }
        }

        private async Task BeforeInvoke(HttpContext context, bool includeHeaders, bool includeRequestBody, string eventTypeName, CancellationToken cancellationToken)
        {
            var auditAction = new AuditApiAction
            {
                IsMiddleware = true,
                UserName = context.User?.Identity.Name,
                IpAddress = context.Connection?.RemoteIpAddress?.ToString(),
                RequestUrl = context.Request.GetDisplayUrl(),
                HttpMethod = context.Request.Method,
                FormVariables = await AuditApiHelper.GetFormVariables(context, cancellationToken),
                Headers = includeHeaders ? AuditApiHelper.ToDictionary(context.Request.Headers) : null,
                ActionName = null,
                ControllerName = null,
                ActionParameters = null,
                RequestBody = new BodyContent
                {
                    Type = context.Request.ContentType,
                    Length = context.Request.ContentLength,
                    Value = includeRequestBody ? await AuditApiHelper.GetRequestBody(context, cancellationToken) : null
                },
                TraceId = context.TraceIdentifier
            };
            var eventType = (eventTypeName ?? "{verb} {url}").Replace("{verb}", auditAction.HttpMethod)
                .Replace("{url}", auditAction.RequestUrl);
            // Create the audit scope
            var auditEventAction = new AuditEventWebApi()
            {
                Action = auditAction
            };
            
            var scopeFactory = context.RequestServices?.GetService<IAuditScopeFactory>() ?? Core.Configuration.AuditScopeFactory;
            var dataProvider = context.RequestServices?.GetService<IAuditDataProvider>() ?? context.RequestServices?.GetService<AuditDataProvider>();

            var auditScope = await scopeFactory.CreateAsync(new AuditScopeOptions()
            {
                EventType = eventType, 
                AuditEvent = auditEventAction,
                DataProvider = dataProvider
            }, cancellationToken);
            context.Items[AuditApiHelper.AuditApiActionKey] = auditAction;
            context.Items[AuditApiHelper.AuditApiScopeKey] = auditScope;
        }

        private async Task AfterInvoke(HttpContext context, bool includeResponseBody, bool includeResponseHeaders, Exception exception)
        {
#pragma warning disable IDE0019 // Use pattern matching
            var auditAction = context.Items[AuditApiHelper.AuditApiActionKey] as AuditApiAction;
            var auditScope = context.Items[AuditApiHelper.AuditApiScopeKey] as AuditScope;
#pragma warning restore IDE0019 // Use pattern matching

            if (auditAction != null && auditScope != null)
            {
                if (exception != null)
                {
                    auditAction.Exception = exception.GetExceptionInfo();
                    auditAction.ResponseStatusCode = 500;
                    auditAction.ResponseStatus = "Internal Server Error";
                }
                else if (context.Response != null)
                {
                    var statusCode = context.Response.StatusCode;
                    auditAction.ResponseStatusCode = statusCode;
                    auditAction.ResponseStatus = AuditApiHelper.GetStatusCodeString(statusCode);
                    if (includeResponseBody && auditAction.ResponseBody == null)
                    {
                        var skipResponse = _config._skipRequestBodyBuilder != null && _config._skipRequestBodyBuilder.Invoke(context);
                        auditAction.ResponseBody = new BodyContent
                        {
                            Type = context.Response.ContentType,
                            Length = context.Response.ContentLength,
                            Value = skipResponse ? null : await AuditApiHelper.GetResponseBody(context, default)
                        };
                    }
                }
                if (includeResponseHeaders)
                {
                    auditAction.ResponseHeaders = AuditApiHelper.ToDictionary(context.Response.Headers);
                }
                // Replace the Action field and save
                auditScope.EventAs<AuditEventWebApi>().Action = auditAction;
                await auditScope.DisposeAsync();
            }
        }
    }
}
#endif
