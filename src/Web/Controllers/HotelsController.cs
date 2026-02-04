using Infrastructure.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Web.DTOs.Hotels;

namespace Web.Controllers
{
    [Route("api/v1/hotels")]
    [ApiController]
    public class HotelsController(IHotelService hotelService) : ControllerBase
    {
        /// <summary>
        /// Search for hotels by name
        /// </summary>
        /// <param name="name">Optional hotel name to filter by (partial match)</param>
        /// <returns>List of hotels matching the search criteria</returns>
        /// <response code="200">Hotels found (may be empty list)</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HotelDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] string? name = null)
        {
            var hotels = await hotelService.Search(name);
            var dtos = hotels.Select(x => new HotelDto { Id = x.Id, Name = x.Name });
            
            return Ok(dtos);
        }
    }
}
