using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Net6.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    [ApiController]
    public class OpenController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] { "Time is: ", DateTime.Now.ToString("s") };
        }
    }
}
