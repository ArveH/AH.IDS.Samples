using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Linq;

namespace Api.Core31.Controllers
{
    [ApiController]
    [Route("identity")]
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
}