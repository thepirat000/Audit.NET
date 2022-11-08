using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Audit.WebApi.Template.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private IValuesService _provider;

        public ValuesController(IValuesService provider)
        {
            _provider = provider;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return Ok(_provider.GetValues());
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id)
        {
            return Ok(await _provider.GetAsync(id));
        }

        // POST api/values
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] string value)
        {
            return Ok(await _provider.InsertAsync(value));
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public async Task Put(int id, [FromBody] string value)
        {
            await _provider.ReplaceAsync(id, value);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> Delete(int id)
        {
            return Ok(await _provider.DeleteAsync(id));
        }

        // DELETE api/values/delete
        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<bool>> Delete([FromBody] string ids)
        {
            return Ok(await _provider.DeleteMultipleAsync(ids.Split(',').Select(s => int.Parse(s)).ToArray()));
        }
    }
}
