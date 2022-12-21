using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.WebApi;
using Microsoft.AspNetCore.JsonPatch;
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

        // PATCH api/my/JsonPatch
        [AuditApi(IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true, IncludeResponseHeaders = true)]
        [HttpPatch("JsonPatch")]
        public IActionResult JsonPatch([FromBody] JsonPatchDocument<Customer> patchDoc)
        {
            return Ok();
        }
    }

}
