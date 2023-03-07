using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using Shared.Net7;
using System.IdentityModel.Tokens.Jwt;

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

            builder.Services.AddSingleton(Log.Logger);

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IDiscoveryCache>(r =>
            {
                var authority = builder.Configuration.GetValue<string>("Auth:Authority");
                var factory = r.GetRequiredService<IHttpClientFactory>();
                return new DiscoveryCache(authority, () => factory.CreateClient());
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = "Client.MVC.Net7";
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");
                    options.ClientId = builder.Configuration.GetValue<string>("Auth:Client");
                    options.ClientSecret = builder.Configuration.GetValue<string>("Auth:ClientSecret");

                    options.ResponseType = "code";

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add(Net7Constants.ApiName);
                    options.Scope.Add("offline_access");

                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                        {
                            var tenant = builder.Configuration.GetValue<string>("auth:tenant");
                            var idpName = builder.Configuration.GetValue<string>("auth:idp");

                            context.ProtocolMessage.AcrValues = $"tenant:{tenant}";
                            if (!string.IsNullOrWhiteSpace(idpName))
                            {
                                context.ProtocolMessage.AcrValues += $" loginidp:{idpName}";
                            }
                            Log.Logger.ForContext<Program>().Information("acr_values: {@AcrValues}", context.ProtocolMessage.AcrValues);
                        }
                        return Task.CompletedTask;
                    };

                });
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSerilogRequestLogging();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .RequireAuthorization();

            app.Run();
        }
    }
}