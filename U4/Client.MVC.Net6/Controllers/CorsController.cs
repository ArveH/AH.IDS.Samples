using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.MVC.Net6.Controllers
{
    public class CorsController : Controller
    {
        private readonly IConfiguration _config;

        public CorsController(IConfiguration config)
        {
            _config = config;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Cors()
        {
            ViewBag.Api = _config.GetValue("Endpoints:Api", "https://localhost:6001");
            await Task.CompletedTask;
            return View();
        }
    }
}
