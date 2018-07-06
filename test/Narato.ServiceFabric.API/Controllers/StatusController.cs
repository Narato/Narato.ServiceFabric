using Microsoft.AspNetCore.Mvc;

namespace Narato.ServiceFabric.API.Controllers
{
    [Route("[controller]")]
    public class StatusController : Controller
    {
        // GET status/ping
        [Route("ping")]
        [HttpGet("ping")]
        public IActionResult GetPing()
        {
            return Ok("Ok");
        }

        // GET status/version
        [Route("version")]
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = "v0.0.0";
            return Ok(version);
        }
    }
}