#if ASP_NET
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
    public class ContextWrapper : IContextWrapper
    {
        /// <summary>
        /// The Http Context
        /// </summary>
        protected HttpContextBase HttpContext = null;
        /// <summary>
        /// The Owin Context
        /// </summary>
        protected IOwinContext OwinContext = null;

        protected bool IsOwin => OwinContext != null;

        public ContextWrapper(HttpRequestMessage request)
        {
            OwinContext = request.GetOwinContext();
            HttpContext = GetHttpContext(request);
        }

        public virtual HttpContextBase GetHttpContext()
        {
            return HttpContext;
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
            var currentContext = System.Web.HttpContext.Current;
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
                dict[k ?? ""] = col[k];
            }
            return dict;
        }

        /// <summary>
        /// Sets a variable in the context
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public virtual void Set<T>(string key, T value) where T : class
        {
            if (IsOwin)
            {
                OwinContext.Set(key, value);
            }
            else
            {
                if (HttpContext != null)
                {
                    HttpContext.Items[key] = value;
                }
            }
        }

        /// <summary>
        /// Gets a variable from the context
        /// </summary>
        public virtual T Get<T>(string key) where T : class
        {
            if (IsOwin)
            {
                return OwinContext.Get<T>(key);
            }
            else
            {
                if (HttpContext != null && HttpContext.Items.Contains(key))
                {
                    return HttpContext.Items[key] as T;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the form variables.
        /// </summary>
        public virtual async Task<IDictionary<string, string>> GetFormVariables()
        {
            if (HttpContext != null)
            {
                IFormCollection formCollection = await HttpContext?.Request?.ReadFormAsync();
                return ToDictionary(formCollection);
            }
            return null;
        }

        /// <summary>
        /// Gets the client IP.
        /// </summary>
        public virtual string GetClientIp()
        {
            if (IsOwin)
            {
                return OwinContext.Request.RemoteIpAddress;
            }
            else
            {
                return HttpContext?.Request?.UserHostAddress;
            }
        }
    }
}
#endif
