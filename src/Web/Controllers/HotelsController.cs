using Infrastructure.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Web.DTOs.Hotels;

namespace Web.Controllers
{
    [Route("api/hotels")]
    [ApiController]
    public class HotelsController(IHotelService hotelService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? name = null)
        {
            var hotels = await hotelService.Search(name);
            var dtos = hotels.Select(x => new HotelDto { Id = x.Id, Name = x.Name });
            
            return Ok(dtos);
        }
    }
}
