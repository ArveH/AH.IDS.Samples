using Api.Net6.Helpers;
using Shared.Net6;

namespace Api.Net6
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddCors();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddAuthentication("token")

                // JWT tokens
                .AddJwtBearer("token", options =>
                {
                    options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");
                    options.Audience = Net6Constants.ApiName;

                    options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
                    options.MapInboundClaims = false;

                    // if token does not contain a dot, it is a reference token
                    options.ForwardDefaultSelector = Selector.ForwardReferenceToken();
                })

                // reference tokens
                .AddOAuth2Introspection("introspection", options =>
                {
                    options.Authority = builder.Configuration.GetValue<string>("Auth:Authority");

                    options.ClientId = Net6Constants.ApiName;
                    options.ClientSecret = builder.Configuration.GetValue<string>("Auth:Authority");
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}