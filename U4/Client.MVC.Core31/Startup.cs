using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using Shared.Core31;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace Client.MVC.Core31
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            Log.Information("Starting up Client.MVC.Core31...");

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services.AddSingleton(Log.Logger);
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = Configuration.GetValue<string>("Auth:Authority");

                    options.ClientId = Configuration.GetValue<string>("Auth:Client");
                    // Note: You don't need a client secret, but it is recommended
                    options.ClientSecret = Configuration.GetValue<string>("Auth:ClientSecret");
                    options.ResponseType = "code";
                    options.UsePkce = true;

                    // 
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add(Core31Constants.ApiName);
                    options.Scope.Add("offline_access");

                    // keeps id_token smaller
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                        {
                            var tenant = Configuration.GetValue<string>("Auth:Tenant");
                            var idp = Configuration.GetValue<string>("Auth:Idp");
                            context.ProtocolMessage.AcrValues =
                                $"tenant:{tenant}";
                            if (!string.IsNullOrWhiteSpace(idp)) context.ProtocolMessage.AcrValues += $" loginidp:{idp}";
                            Log.Logger.ForContext<Program>().Information("acr_values: {AcrValues}", context.ProtocolMessage.AcrValues);
                        }

                        return Task.CompletedTask;
                    };
                });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}")
                    .RequireAuthorization();
            });
        }
    }
}
