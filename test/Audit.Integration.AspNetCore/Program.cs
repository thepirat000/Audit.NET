using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.WebApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Audit.Integration.AspNetCore
{
    public class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var ct = new CancellationTokenSource();
            var pc = Console.ForegroundColor;
            var runner = BuildWebHost(args).RunAsync(ct.Token);
            
            var webApiTests = new WebApiTests(27050);
            var mvcTests = new MvcTests(27050);

            try
            {
                Console.WriteLine("START - TestInitialize");
                await webApiTests.TestInitialize();
                Console.WriteLine("PASSED - TestInitialize");

                Console.WriteLine("START - Test_WebApi_CreationPolicy_InsertOnStartInsertOnEnd");
                await webApiTests.Test_WebApi_CreationPolicy_InsertOnStartInsertOnEnd();
                Console.WriteLine("PASSED - Test_WebApi_CreationPolicy_InsertOnStartInsertOnEnd");

                Console.WriteLine("START - Test_WebApi_CreationPolicy_InsertOnEnd");
                await webApiTests.Test_WebApi_CreationPolicy_InsertOnEnd();
                Console.WriteLine("PASSED - Test_WebApi_CreationPolicy_InsertOnEnd");

                Console.WriteLine("START - Test_WebApi_CreationPolicy_InsertOnStartReplaceOnEnd");
                await webApiTests.Test_WebApi_CreationPolicy_InsertOnStartReplaceOnEnd();
                Console.WriteLine("PASSED - Test_WebApi_CreationPolicy_InsertOnStartReplaceOnEnd");

                Console.WriteLine("START - Test_WebApi_CreationPolicy_Manual");
                await webApiTests.Test_WebApi_CreationPolicy_Manual();
                Console.WriteLine("PASSED - Test_WebApi_CreationPolicy_Manual");

                Console.WriteLine("START - Test_WebApi_AuditApiGlobalAttributeOrder");
                await webApiTests.Test_WebApi_AuditApiGlobalAttributeOrder();
                Console.WriteLine("PASSED - Test_WebApi_AuditApiGlobalAttributeOrder");

                Console.WriteLine("START - Test_WebApi_AuditApiAttributeOrder");
                await webApiTests.Test_WebApi_AuditApiAttributeOrder();
                Console.WriteLine("PASSED - Test_WebApi_AuditApiAttributeOrder");

                Console.WriteLine("START - Test_WebApi_TestFromServiceIgnore");
                await webApiTests.Test_WebApi_TestFromServiceIgnore();
                Console.WriteLine("PASSED - Test_WebApi_TestFromServiceIgnore");

                Console.WriteLine("START - Test_WebApi_ResponseHeaders_Attribute");
                await webApiTests.Test_WebApi_ResponseHeaders_Attribute();
                Console.WriteLine("PASSED - Test_WebApi_ResponseHeaders_Attribute");

                Console.WriteLine("START - Test_WebApi_ResponseHeaders_GlobalFilter");
                await webApiTests.Test_WebApi_ResponseHeaders_GlobalFilter();
                Console.WriteLine("PASSED - Test_WebApi_ResponseHeaders_GlobalFilter");

                Console.WriteLine("START - Test_WebApi_ResponseHeaders_Middleware");
                await webApiTests.Test_WebApi_ResponseHeaders_Middleware();
                Console.WriteLine("PASSED - Test_WebApi_ResponseHeaders_Middleware");
                

                Console.WriteLine("START - Test_WebApi_Post_Async");
                await webApiTests.Test_WebApi_Post_Async();
                Console.WriteLine("PASSED - Test_WebApi_Post_Async");

                Console.WriteLine("START - Test_WebApi_DoubleActionFilter");
                await webApiTests.Test_WebApi_DoubleActionFilter();
                Console.WriteLine("PASSED - Test_WebApi_DoubleActionFilter");

                Console.WriteLine("START - Test_WebApi_Middleware_NoResponseBody");
                await webApiTests.Test_WebApi_Middleware_NoResponseBody();
                Console.WriteLine("PASSED - Test_WebApi_Middleware_NoResponseBody");

                Console.WriteLine("START - Test_WebApi_Middleware_WrongData");
                await webApiTests.Test_WebApi_Middleware_WrongData();
                Console.WriteLine("PASSED - Test_WebApi_Middleware_WrongData");

                Console.WriteLine("START - Test_WebApi_Middleware_WrongRoute");
                await webApiTests.Test_WebApi_Middleware_WrongRoute();
                Console.WriteLine("PASSED - Test_WebApi_Middleware_WrongRoute");

                Console.WriteLine("START - Test_WebApi_Middleware_Exception");
                await webApiTests.Test_WebApi_Middleware_Exception();
                Console.WriteLine("PASSED - Test_WebApi_Middleware_Exception");

                Console.WriteLine("START - Test_WebApi_Middleware_Mix_Filter");
                await webApiTests.Test_WebApi_Middleware_Mix_Filter();
                Console.WriteLine("PASSED - Test_WebApi_Middleware_Mix_Filter");

                Console.WriteLine("START - Test_WebApi_Middleware_Alone");
                await webApiTests.Test_WebApi_Middleware_Alone();
                Console.WriteLine("PASSED - Test_WebApi_Middleware_Alone");

                Console.WriteLine("START - Test_WebApi_AuditIgnoreAttribute_Action_Async");
                await webApiTests.Test_WebApi_AuditIgnoreAttribute_Action_Async();
                Console.WriteLine("PASSED - Test_WebApi_AuditIgnoreAttribute_Action_Async");

                Console.WriteLine("START - Test_WebApi_AuditIgnoreAttribute_Param_Async");
                await webApiTests.Test_WebApi_AuditIgnoreAttribute_Param_Async();
                Console.WriteLine("PASSED - Test_WebApi_AuditIgnoreAttribute_Param_Async");

                Console.WriteLine("START - Test_WebApi_FormCollectionLimit_Async");
                await webApiTests.Test_WebApi_FormCollectionLimit_Async();
                Console.WriteLine("PASSED - Test_WebApi_FormCollectionLimit_Async");
                
                Console.WriteLine("START - Test_WebApi_GlobalFilter_Async");
                await webApiTests.Test_WebApi_GlobalFilter_Async();
                Console.WriteLine("PASSED - Test_WebApi_GlobalFilter_Async");

                Console.WriteLine("START - Test_WebApi_FilterResponseBody_Included");
                await webApiTests.Test_WebApi_FilterResponseBody_Included();
                Console.WriteLine("PASSED - Test_WebApi_FilterResponseBody_Included");

                Console.WriteLine("START - Test_Mvc_Exception_Async");
                await mvcTests.Test_Mvc_Exception_Async();
                Console.WriteLine("PASSED - Test_Mvc_Exception_Async");

                Console.WriteLine("START - Test_Mvc_HappyPath_Async");
                await mvcTests.Test_Mvc_HappyPath_Async();
                Console.WriteLine("PASSED - Test_Mvc_HappyPath_Async");

                Console.WriteLine("START - Test_Mvc_Ignore");
                await mvcTests.Test_Mvc_Ignore();
                Console.WriteLine("PASSED - Test_Mvc_Ignore");

                Console.WriteLine("START - Test_WebApi_HappyPath_Async");
                await webApiTests.Test_WebApi_HappyPath_Async();
                Console.WriteLine("PASSED - Test_WebApi_HappyPath_Async");
                
                Console.WriteLine("START - Test_WebApi_Exception_Async");
                await webApiTests.Test_WebApi_Exception_Async();
                Console.WriteLine("PASSED - Test_WebApi_Exception_Async");

                Console.WriteLine("START - Test_Mvc_AuditIgnoreAttribute_Mix_Middleware_Async");
                await mvcTests.Test_Mvc_AuditIgnoreAttribute_Middleware_Async();
                Console.WriteLine("PASSED - Test_Mvc_AuditIgnoreAttribute_Mix_Middleware_Async");

                Console.WriteLine("START - Test_WebApi_AuditIgnoreAttribute_Mix_Middleware_Async");
                await webApiTests.Test_WebApi_AuditIgnoreAttribute_Mix_Middleware_Async();
                Console.WriteLine("PASSED - Test_WebApi_AuditIgnoreAttribute_Mix_Middleware_Async");

                Console.WriteLine("START - Test_WebApi_AuditIgnoreAttribute_Middleware_AuditIgnoreFilter_Async");
                await webApiTests.Test_WebApi_AuditIgnoreAttribute_Middleware_AuditIgnoreFilter_Async();
                Console.WriteLine("PASSED - Test_WebApi_AuditIgnoreAttribute_Middleware_AuditIgnoreFilter_Async");
                

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nALL TESTS PASSED");
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nSOME TEST FAILED");
                throw;
            }
            finally
            {
                Console.ForegroundColor = pc;
            }

            ct.Cancel();
            await runner;
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
