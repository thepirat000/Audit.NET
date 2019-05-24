#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Audit.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Audit.WebApi
{
    /// <summary>
    /// Action filter that can be registered on the MVC pipeline to allow using [AuditIgnoreAttribute] along with the middleware.
    /// </summary>
    public class AuditIgnoreActionFilter : IActionFilter
    {
        private readonly AuditApiAdapter _adapter = new AuditApiAdapter();
        public virtual void OnActionExecuted(ActionExecutedContext context)
        {
        }
        public virtual void OnActionExecuting(ActionExecutingContext context)
        {
            // this will discard the event if it's ignored
            _adapter.ActionIgnored(context);
        }
    }
}
#endif