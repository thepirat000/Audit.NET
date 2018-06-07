using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            //BuildWebHost(args).RunAsync().GetAwaiter().GetResult();
            //return;

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

                Console.WriteLine("START - Test_WebApi_FormCollectionLimit_Async");
                await webApiTests.Test_WebApi_FormCollectionLimit_Async();
                Console.WriteLine("PASSED - Test_WebApi_FormCollectionLimit_Async");

                Console.WriteLine("START - Test_WebApi_GlobalFilter_Async");
                await webApiTests.Test_WebApi_GlobalFilter_Async();
                Console.WriteLine("PASSED - Test_WebApi_GlobalFilter_Async");

                Console.WriteLine("START - Test_WebApi_FilterResponseBody_Included");
                await webApiTests.Test_WebApi_FilterResponseBody_Included();
                Console.WriteLine("PASSED - Test_WebApi_FilterResponseBody_Included");

                Console.WriteLine("START - Test_WebApi_Post_Async");
                await webApiTests.Test_WebApi_Post_Async();
                Console.WriteLine("PASSED - Test_WebApi_Post_Async");

                Console.WriteLine("START - Test_Mvc_Exception_Async");
                await mvcTests.Test_Mvc_Exception_Async();
                Console.WriteLine("PASSED - Test_Mvc_Exception_Async");

                Console.WriteLine("START - Test_Mvc_HappyPath_Async");
                await mvcTests.Test_Mvc_HappyPath_Async();
                Console.WriteLine("PASSED - Test_Mvc_HappyPath_Async");

                Console.WriteLine("START - Test_WebApi_HappyPath_Async");
                await webApiTests.Test_WebApi_HappyPath_Async();
                Console.WriteLine("PASSED - Test_WebApi_HappyPath_Async");

                Console.WriteLine("START - Test_WebApi_Exception_Async");
                await webApiTests.Test_WebApi_Exception_Async();
                Console.WriteLine("PASSED - Test_WebApi_Exception_Async");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nALL TESTS PASSED");
            }
            catch (Exception e)
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
