using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

namespace Client.MVC.Core31
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((context, config) =>
                {
                    var path = context.Configuration.GetValue<string>("LogFileFullPath");
                    config
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.File(
                            path,
                            fileSizeLimitBytes: 50_000_000,
                            rollOnFileSizeLimit: true,
                            shared: true,
                            flushToDiskInterval: TimeSpan.FromSeconds(1),
                            outputTemplate:
                            "{Timestamp:yyyyMMdd HH:mm:ss.fff} [{Level:u3}] {Message:lj}  {Properties:j} {Exception}{NewLine}");
                });
    }
}
