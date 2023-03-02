using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Client.MVC.Net6.Controllers;

public class CorsController : Controller
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;

    public CorsController(
        IConfiguration config,
            ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Cors()
    {
        var token = await HttpContext.GetTokenAsync("access_token");

        ViewBag.Token = token??"";
        ViewBag.Api = _config.GetValue("Endpoints:Api", "https://localhost:6001");
        return View();
    }
}