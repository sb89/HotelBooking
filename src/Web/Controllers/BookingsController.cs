using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create()
        {
            return Created("api/bookings", "test");
        }

        [HttpGet("{bookingReference:int}")]
        public async Task<IActionResult> Get(int bookingReference)
        {
            return Ok();
        }
    }
}
