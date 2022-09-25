#if ASP_NET
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace Audit.WebApi
{
    public interface IContextWrapper
    {
        /// <summary>
        /// Gets a variable from the context
        /// </summary>
        T Get<T>(string key) where T : class;
        /// <summary>
        /// Gets the client IP.
        /// </summary>
        string GetClientIp();
        /// <summary>
        /// Gets the form variables.
        /// </summary>
        async Task<IDictionary<string, string>> GetFormVariables();
        /// <summary>
        /// Gets the HttpContext
        /// </summary>
        /// <returns></returns>
        HttpContextBase GetHttpContext();
        /// <summary>
        /// Sets a variable in the context
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void Set<T>(string key, T value) where T : class;
    }
}
#endif
