using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Api.Net7.Controllers;

[Route("[controller]")]
[ApiController]
public class IdentityController : ControllerBase
{
    private readonly ILogger _logger;

    public IdentityController(ILogger logger)
    {
        _logger = logger.ForContext<IdentityController>();
    }

    [HttpGet]
    public ActionResult Get()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        _logger.Information("claims: {@claims}", claims);

        return new JsonResult(claims);
    }
}