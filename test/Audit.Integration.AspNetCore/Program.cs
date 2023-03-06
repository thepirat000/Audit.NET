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
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSetting("IsMvc", "true");
                    webBuilder.UseSetting("IsWebApi", "true");
                    webBuilder.UseStartup<Startup>();
                })
                .Build()
                .Run();
        }
    }
}
