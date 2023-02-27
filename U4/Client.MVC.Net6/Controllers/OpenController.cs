using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.MVC.Net6.Controllers
{
    [AllowAnonymous]
    public class OpenController : Controller
    {
        private readonly IConfiguration _config;

        public OpenController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> CallOpenEndpoints()
        {
            ViewBag.Api = _config.GetValue("Endpoints:Api", "https://localhost:6001");
            await Task.CompletedTask;
            return View();
        }
    }
}
