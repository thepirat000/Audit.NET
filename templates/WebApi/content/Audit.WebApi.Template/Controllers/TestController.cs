using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.WebApi.Template.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public TestController(IServiceProvider serviceProvider)
        {
            _hostingEnvironment = serviceProvider.GetRequiredService<IHostingEnvironment>();
        }
        [HttpGet()]
        public IActionResult Test()
        {
            var assembly = Assembly.GetExecutingAssembly().FullName;
            var result = new
            {
                Assembly = assembly,
                Environment = _hostingEnvironment.EnvironmentName,
                MachineName = Environment.MachineName,
                OS = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})"
            };
            return Ok(result);
        }
    }
}