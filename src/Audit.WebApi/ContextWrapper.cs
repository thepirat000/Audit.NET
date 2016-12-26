#if NET45
using Microsoft.Owin;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Web;

namespace Audit.WebApi
{
    /// <summary>
    /// Wrapper around request context to handle both HttpContext and OwinContext
    /// </summary>
    internal class ContextWrapper
    {
        private HttpContextBase _httpContext = null;
        private IOwinContext _owinContext = null;
        internal bool IsOwin => _owinContext != null;

        public ContextWrapper(HttpRequestMessage request)
        {
            _owinContext = request.GetOwinContext();
            _httpContext = GetHttpContext(request);
        }

        private HttpContextBase GetHttpContext(HttpRequestMessage request)
        {
            HttpContextBase context = null;
            if (request?.Properties != null && request.Properties.ContainsKey("MS_HttpContext"))
            {
                object obj;
                request.Properties.TryGetValue("MS_HttpContext", out obj);
                context = obj as HttpContextBase;
                if (context != null)
                {
                    return context;
                }
            }
            var currentContext = HttpContext.Current;
            return currentContext == null ? null : new HttpContextWrapper(currentContext);
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

        /// <summary>
        /// Sets a variable in the context
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set<T>(string key, T value) where T : class
        {
            if (IsOwin)
            {
                _owinContext.Set(key, value);
            }
            else
            {
                if (_httpContext != null)
                {
                    _httpContext.Items[key] = value;
                }
            }
        }

        /// <summary>
        /// Gets a variable from the context
        /// </summary>
        public T Get<T>(string key) where T : class
        {
            if (IsOwin)
            {
                return _owinContext.Get<T>(key);
            }
            else
            {
                if (_httpContext != null && _httpContext.Items.Contains(key))
                {
                    return _httpContext.Items[key] as T;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the form variables.
        /// </summary>
        public IDictionary<string, string> GetFormVariables()
        {
            if (_httpContext != null)
            {
                return ToDictionary(_httpContext?.Request?.Form);
            }
            return null;
        }

        /// <summary>
        /// Gets the client IP.
        /// </summary>
        public string GetClientIp()
        {
            if (IsOwin)
            {
                return _owinContext.Request.RemoteIpAddress;
            }
            else
            {
                return _httpContext?.Request?.UserHostAddress;
            }
        }
    }
}
#endif