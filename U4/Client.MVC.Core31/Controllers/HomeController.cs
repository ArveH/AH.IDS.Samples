﻿using Client.MVC.Core31.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Serilog;
using Shared.Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Client.MVC.Core31.Controllers
{
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

            var client = _httpClientFactory.CreateClient();
            client.SetBearerToken(token);

            var apiUrl = _config.GetValue<string>("Endpoints:Api", "") + "/identity";
            _logger.Information("Requesting '{apiUrl}'", apiUrl);
            var response = await client.GetStringAsync(apiUrl);
            ViewBag.Json = response.PrettyPrintJson();
            _logger.Information("Response from API: {json}", ViewBag.Json);
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
                if (info.Properties == null) throw new ArgumentNullException(nameof(info.Properties));

                info.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                info.Properties.UpdateTokenValue("access_token", newAccessToken);
                info.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

                if (info.Principal == null) throw new ArgumentNullException(nameof(info.Principal));
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
}
