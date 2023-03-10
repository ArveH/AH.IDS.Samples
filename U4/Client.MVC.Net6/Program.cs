using System.IdentityModel.Tokens.Jwt;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using Shared.Net6;


namespace Client.MVC.Net6;

public class Program
{
    public static void Main(string[] args)
    {
        IdentityModelEventSource.ShowPII = true;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        Log.Information("Starting up Client.MVC.Net6...");

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
                // Not necessary, but makes it easier to see "what's what" in browser tools
                options.Cookie.Name = "client.mvc.net6";
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");

                options.ClientId = builder.Configuration.GetValue<string>("Auth:Client");
                // Note: You don't need a client secret, but it is recommended
                options.ClientSecret = builder.Configuration.GetValue<string>("Auth:ClientSecret");
                options.ResponseType = "code";

                // 
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add(Net6Constants.ApiName);
                options.Scope.Add("offline_access");

                // keeps id_token smaller
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                // We use the query parameter "acr_values" to set tenant information
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                    {
                        var tenant = builder.Configuration.GetValue<string>("Auth:Tenant");
                        var idp = builder.Configuration.GetValue<string>("Auth:Idp");
                        context.ProtocolMessage.AcrValues =
                            $"tenant:{tenant}";
                        if (!string.IsNullOrWhiteSpace(idp)) context.ProtocolMessage.AcrValues += $" loginidp:{idp}";
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
                "default",
                "{controller=Home}/{action=Index}/{id?}")
            .RequireAuthorization(); // Disable anonymous access for the entire application

        app.Run();
    }
}