using System.Diagnostics;
using System.Globalization;
using Client.MVC.Net6.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.MVC.Net6.Controllers;

public class HomeController : Controller
{
    private readonly IDiscoveryCache _discoveryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        IDiscoveryCache discoveryCache,
        ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _discoveryCache = discoveryCache;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Secure()
    {
        return View();
    }

    public IActionResult Logout()
    {
        return SignOut(
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> RenewTokens()
    {
        var disco = await _discoveryCache.GetAsync();
        if (disco.IsError) throw new Exception(disco.Error);

        var rt = await HttpContext.GetTokenAsync("refresh_token");
        var tokenClient = _httpClientFactory.CreateClient();
        var clientId = _config.GetValue<string>("Auth:Client");
        var clientSecret = _config.GetValue<string>("Auth:ClientSecret");

        var tokenResult = await tokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshToken = rt
        });

        if (!tokenResult.IsError)
        {
            // Just in case you want to look at the old token during debugging
            var _ = await HttpContext.GetTokenAsync("id_token");
            var newAccessToken = tokenResult.AccessToken;
            var newRefreshToken = tokenResult.RefreshToken;
            var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);

            var info = await HttpContext.AuthenticateAsync("Cookies");
            ArgumentNullException.ThrowIfNull(info.Properties);

            info.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
            info.Properties.UpdateTokenValue("access_token", newAccessToken);
            info.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

            ArgumentNullException.ThrowIfNull(info.Principal);
            await HttpContext.SignInAsync("Cookies", info.Principal, info.Properties);
            return Redirect("~/Home/Secure");
        }

        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorMessage = tokenResult.Error
        });
    }
}