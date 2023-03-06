using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Audit.Integration.AspNetCore
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}
