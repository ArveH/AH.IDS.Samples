using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Api.Net6.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    [ApiController]
    public class OpenController : ControllerBase
    {
        private readonly ILogger _logger;

        public OpenController(ILogger logger)
        {
            _logger = logger.ForContext<OpenController>();
        }

        [HttpGet]
        public IEnumerable<string> SimpleRequest()
        {
            // A Simple Requests sends the "Origin" header,
            // and the server responds with the "Access-Control-Allow-Origin" header 
            // Note: When responding to a credentialed requests request, the server
            //       must specify an origin in the value of the Access-Control-Allow-Origin
            //       header, instead of specifying the "*" wildcard.
            var now = DateTime.Now.ToString("s");
            _logger.Information("Responding with {Now}. Origin: {Origin}. AccessControlAllowOrigin: *",
                now, Request.Headers.Origin);
            Response.Headers.AccessControlAllowOrigin = "*";
            Response.Headers.AccessControlExposeHeaders = "*";
            return new[] { "Simple Request: ", now };
        }
    }
}
