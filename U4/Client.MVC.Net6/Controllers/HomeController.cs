using Client.MVC.Net6.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using ILogger = Serilog.ILogger;

namespace Client.MVC.Net6.Controllers;

public class HomeController : Controller
{
    private readonly IDiscoveryCache _discoveryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger _logger;

    public HomeController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        IDiscoveryCache discoveryCache,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _discoveryCache = discoveryCache;
        _logger = logger.ForContext<HomeController>();
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

    public async Task<IActionResult> CallApiJS()
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.Error("Can't get Access token when CallApiJS");
            throw new ArgumentException(nameof(token));
        }

        ViewBag.Token = token;
        ViewBag.Path = _config.GetValue("Endpoints:Api", "https://localhost:6001") + "/identity";
        return View();
    }

    public IActionResult Logout()
    {
        _logger.Information("Logging out...");
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

    public async Task<IActionResult> CallApi()
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.Error("Can't get Access token when CallApi");
            throw new ArgumentException(nameof(token));
        }

        var apiUrl = _config.GetValue("Endpoints:Api", "") + "/identity";
        _logger.Information("Requesting '{apiUrl}'", apiUrl);
        var msg = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        msg.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);

        var client = _httpClientFactory.CreateClient();

        _logger.Information("Requesting '{@Msg}'", msg);
        var response = await client.SendAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            _logger.Error(response.ReasonPhrase??"No ResponsePhrase");
            var contentStr = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(contentStr))
                _logger.Error(contentStr);
            return View();
        }

        var json = await response.Content.ReadAsStringAsync();
        ViewBag.Json = json.PrettyPrintJson();
        if (string.IsNullOrWhiteSpace(ViewBag.Json))
        {
            _logger.Error("No response from API");
        }
        else
        {
            _logger.Information("Response from API: {json}", ViewBag.Json);
        }

        return View();
    }

    public async Task<IActionResult> RenewTokens()
    {
        var disco = await _discoveryCache.GetAsync();
        if (disco.IsError)
        {
            _logger.Error("Can't get Discovery document");
            throw new Exception(disco.Error);
        }

        var rt = await HttpContext.GetTokenAsync("refresh_token");
        var tokenClient = _httpClientFactory.CreateClient();
        var clientId = _config.GetValue<string>("Auth:Client");
        var clientSecret = _config.GetValue<string>("Auth:ClientSecret");
        _logger.Information("Token request: Endpoint='{TokenEndpoint}', Token='{RefreshToken}', ClientId='{ClientId}', Secret={Secret}",
            disco.TokenEndpoint, rt, clientId, $"{clientSecret[..3]}...{clientSecret[^3..]}");

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

        _logger.Error("Renew token failed: {errorMsg}", tokenResult.Error);

        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorMessage = tokenResult.Error
        });
    }
}