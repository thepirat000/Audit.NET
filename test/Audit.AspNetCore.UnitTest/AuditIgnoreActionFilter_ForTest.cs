using Audit.WebApi;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Audit.AspNetCore.UnitTest
{
    public class AuditIgnoreActionFilter_ForTest : AuditIgnoreActionFilter
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.QueryString.HasValue && context.HttpContext.Request.QueryString.Value.Contains("ignorefilter"))
            {
                base.OnActionExecuting(context);
            }
        }
    }
}