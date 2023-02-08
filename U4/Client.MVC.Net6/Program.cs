using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.VisualBasic;

namespace Client.MVC.Net6
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");

                    options.ClientId = builder.Configuration.GetValue<string>("Auth:Client");
                    options.ClientSecret = builder.Configuration.GetValue<string>("Auth:ClientSecret");
                    options.ResponseType = "code";

                    // 
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");

                    // Save the tokens in a cookie
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
                            if (!string.IsNullOrWhiteSpace(idp))
                            {
                                context.ProtocolMessage.AcrValues += $" loginidp:{idp}";
                            }
                        }
                        return Task.CompletedTask;
                    };
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .RequireAuthorization(); // Disable anonymous access for the entire application

            app.Run();
        }
    }
}