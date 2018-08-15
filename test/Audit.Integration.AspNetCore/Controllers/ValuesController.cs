using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Audit.WebApi;
using Microsoft.AspNetCore.Http;
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
