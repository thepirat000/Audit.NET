using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Audit.AspNetCore.UnitTest
{
    public class ApiErrorHandlerMiddleware_Test
    {
        private readonly RequestDelegate _next;

        public ApiErrorHandlerMiddleware_Test(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var e = ex;

            }
            finally
            {
                await context.Response.WriteAsync("ApiErrorHandlerMiddleware");
            }
             
        }
    }
}