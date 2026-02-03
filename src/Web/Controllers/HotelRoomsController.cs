using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/hotels/{hotelId:int}/rooms")]
    [ApiController]
    public class HotelRoomsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(int hotelId, [FromQuery]DateOnly checkIn, [FromQuery]DateOnly checkOut, [FromQuery]int guests)
        {
            return Ok();
        }
    }
}
