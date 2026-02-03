using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            return NoContent();
        }
        
        [HttpPost]
        public async Task<IActionResult> Reset()
        {
            return NoContent();
        }
    }
}
