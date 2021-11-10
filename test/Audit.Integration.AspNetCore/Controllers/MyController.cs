using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Audit.Integration.AspNetCore.Controllers
{
    [AuditApi]
    [Route("api/[controller]")]
    public class MyController : Controller
    {
        // GET api/my
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var scope = this.GetCurrentAuditScope();
            if (scope != null)
            {
                scope.Event.CustomFields["ScopeExists"] = true;
            }
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
        }
    }
}
