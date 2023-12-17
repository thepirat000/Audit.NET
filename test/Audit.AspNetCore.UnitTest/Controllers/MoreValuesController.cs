using System.Collections.Generic;
using Audit.WebApi;
using Microsoft.AspNetCore.Mvc;

namespace Audit.AspNetCore.UnitTest.Controllers
{
    [AuditApi(EventTypeName = "FromControllerAttribute", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
    [Route("api/[controller]")]
    public class MoreValuesController : Controller
    {
        [HttpGet]
        [AuditApi(EventTypeName = "FromActionAttribute", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2", "value3" };
        }

    }
}
