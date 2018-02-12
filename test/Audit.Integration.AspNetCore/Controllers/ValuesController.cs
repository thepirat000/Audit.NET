using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.WebApi;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Integration.AspNetCore.Controllers
{
    public class Request
    {
        public string Value { get; set; }
    }
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [AuditApi(EventTypeName = "api/values", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            await Task.Delay(1);
            if (id == 666)
            {
                throw new Exception("this is a test exception");
            }
            return $"{id}";
        }

        // POST api/values
        [HttpPost]
        [AuditApi(EventTypeName = "api/values/post", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        public IActionResult Post([FromBody]Request request)
        {
            return Ok(request.Value);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
