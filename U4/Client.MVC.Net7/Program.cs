using Microsoft.IdentityModel.Logging;
using Serilog;

namespace Client.MVC.Net7
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IdentityModelEventSource.ShowPII = true;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Starting up Client.MVC.Net7...");

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((context, config) =>
            {
                var path = context.Configuration.GetValue<string>("LogFileFullPath");
                ArgumentException.ThrowIfNullOrEmpty(nameof(path));
                config
                    .ReadFrom.Configuration(context.Configuration)
                    .WriteTo.File(
                        path!,
                        fileSizeLimitBytes: 50_000_000,
                        rollOnFileSizeLimit: true,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1),
                        outputTemplate:
                        "{Timestamp:yyyyMMdd HH:mm:ss.fff} [{Level:u3}] {Message:lj}  {Properties:j} {Exception}{NewLine}");
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}