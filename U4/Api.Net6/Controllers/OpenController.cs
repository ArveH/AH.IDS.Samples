using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            return new [] { "value1", "value2" };
        }
    }
}
