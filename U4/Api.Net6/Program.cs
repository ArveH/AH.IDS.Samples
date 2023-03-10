using Api.Net6.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Serilog;
using Shared.Net6;

namespace Api.Net6
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IdentityModelEventSource.ShowPII = true;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            Log.Information("Starting up Api.Net6...");

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((context, config) =>
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

            builder.Services.AddSingleton(Log.Logger);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyAllowSpecificOrigins",
                    policy =>
                    {
                        policy.WithOrigins(
                                "https://localhost:7123"
                                )
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
            builder.Services.AddControllers();

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddAuthentication("token")

                // JWT tokens
                .AddJwtBearer("token", options =>
                {
                    var authority = builder.Configuration.GetValue<string>("Auth:Authority");
                    options.Authority = authority;
                    options.Audience = Net6Constants.ApiName;

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Log.Error(context.Exception, "Authentication failed");
                            return Task.CompletedTask;
                        }
                    };

                    options.TokenValidationParameters.ValidTypes = new[] { "JWT", "at+jwt" };

                    options.MapInboundClaims = false;

                    // if token does not contain a dot, it is a reference token
                    options.ForwardDefaultSelector = Selector.ForwardReferenceToken();
                })

                // reference tokens
                .AddOAuth2Introspection("introspection", options =>
                {
                    options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");

                    options.ClientId = Net6Constants.ApiName;
                    options.ClientSecret = builder.Configuration.GetValue<string>("ApiSecret");
                });

            var app = builder.Build();

            app.Use(async (ctx, next) =>
            {
                Log.Logger.ForContext<Program>().LogHeaders(
                    "Request headers:", ctx.Request.Headers);
                await next();
                Log.Logger.ForContext<Program>().LogHeaders(
                    "Response headers:", ctx.Response.Headers);
            });
            // Configure the HTTP request pipeline.
            app.UseCors("MyAllowSpecificOrigins");
            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers()
                .RequireAuthorization();

            app.Run();
        }
    }
}