using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Audit.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Audit.Integration.AspNetCore.Controllers
{

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        [AuditIgnore]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet("TestFromServiceIgnore")]
        [AuditApi(IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        public async Task<IActionResult> TestFromServiceIgnore([FromServices] IServiceProvider provider, string t, CancellationToken cancellationToken)
        {
            return Ok(t);
        }

        [HttpPost("FileUpload")]
        [AuditApi(IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        public async Task<IActionResult> FileUpload(ICollection<IFormFile> files)
        {
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    using (var fileStream = new FileStream(Path.Combine(@"d:\temp\", file.FileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                }
            }
            return Ok();
        }

        [HttpPost("GlobalAudit")]
        public async Task<IActionResult> GlobalAudit([AuditIgnore][FromBody]Request request)
        {
            await Task.Delay(0);
            return Ok(request.Value);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if ((context.ActionDescriptor as ControllerActionDescriptor).ActionName == "GlobalAudit")
            {
                var scope = this.GetCurrentAuditScope();
                if (scope != null)
                {
                    scope.Event.CustomFields["ScopeExists"] = true;
                }
            }
            base.OnActionExecuting(context);
        }

        [HttpPost("TestForm")]
        public async Task<IActionResult> TestForm()
        {
            await Task.Delay(0);
            return Ok();
        }

        [HttpPost("TestIgnoreParam")]
        public async Task<IActionResult> TestIgnoreParam(string user, [AuditIgnore][FromQuery(Name = "pass")] string password)
        {
            await Task.Delay(0);
            return Ok();
        }

        [HttpPost("TestIgnoreAction")]
        [AuditIgnore]
        public async Task<IActionResult> TestIgnoreAction()
        {
            await Task.Delay(0);
            return Ok("hi");
        }

        [HttpPost("TestNormal")]
        public async Task<IActionResult> TestNormal()
        {
            await Task.Delay(0);
            return Ok("hi");
        }

        [HttpPost("TestIgnoreResponse")]
        [return:AuditIgnore]
        public async Task<IActionResult> TestIgnoreResponse([FromBody] Request request)
        {
            await Task.Delay(0);
            return Ok("hi");
        }

        [HttpPost("TestIgnoreResponseFilter")]
        [AuditApi(IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        [return: AuditIgnore]
        public async Task<IActionResult> TestIgnoreResponseFilter([FromBody] Request request)
        {
            await Task.Delay(0);
            return Ok("hi from filter");
        }

        [AuditIgnore] // ignored here but will be picked up by the middleware
        [HttpPost("TestResponseHeaders")]
        public async Task<IActionResult> TestResponseHeaders(string id)
        {
            await Task.Delay(0);
            HttpContext.Response.Headers.Add("some-header", id);
            return Ok();
        }

        [HttpPost("TestResponseHeadersGlobalFilter")]
        public async Task<IActionResult> TestResponseHeadersGlobalFilter(string id)
        {
            await Task.Delay(0);
            HttpContext.Response.Headers.Add("some-header-global", id);
            return Ok();
        }

        [AuditApi(IncludeResponseHeaders = true)]
        [HttpPost("TestResponseHeadersAttribute")]
        public async Task<IActionResult> TestResponseHeadersAttribute(string id)
        {
            await Task.Delay(0);
            HttpContext.Response.Headers.Add("some-header-attr", id);
            return Ok();
        }

        [AuditApi(EventTypeName = "api/values", IncludeHeaders = true, IncludeResponseBody = true, IncludeResponseBodyFor = new[] { HttpStatusCode.BadRequest }, IncludeRequestBody = true, IncludeModelState = true)]
        [HttpGet("hi/{id}")]
        public async Task<IActionResult> HiGet(int id)
        {
            await Task.Delay(1);
            if (id == 142857)
            {
                return BadRequest("this is a bad request test");
            }
            return Ok($"hi {id}");
        }

        // GET api/values/5
        [AuditApi(EventTypeName = "api/values", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            await Task.Delay(1);
            if (id == 666)
            {
                throw new Exception("*************** THIS IS A TEST EXCEPTION **************");
            }
            return $"{id}";
        }

        // POST api/values
        [HttpPost]
        [AuditApi(EventTypeName = "api/values/post", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        public IActionResult Post([FromBody]Request request)
        {
            HttpContext.Response.Headers.Add("some-header-ignored", "123");
            return Ok(request.Value);
        }

        [AuditApi(EventTypeName = "api/values/PostMix", IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
        [HttpPost("PostMix")]
        public IActionResult PostMix([FromBody]Request request, [FromQuery]string middleware)
        {
            return Ok(request.Value);
        }

        [HttpPost("PostMiddleware")]
        public IActionResult PostMiddleware([FromBody]Request request)
        {
            if (request.Value == "666")
            {
                throw new Exception("THIS IS A TEST EXCEPTION 666");
            }
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
