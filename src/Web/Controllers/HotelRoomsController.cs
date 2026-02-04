using FluentValidation;
using Infrastructure.Interfaces.Services;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Web.DTOs.HotelRooms;
using Web.Extensions;

namespace Web.Controllers
{
    [Route("api/v1/hotels/{hotelId:int}/rooms")]
    [ApiController]
    public class HotelRoomsController(IHotelRoomsService hotelRoomsService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> SearchAvailable(int hotelId, [FromQuery]SearchAvailableRequest request,
            IValidator<SearchAvailableRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return validationResult.ToValidationProblem(this);
            
            var criteria = new SearchAvailableCriteria
            {
                HotelId =  hotelId,
                StartDate =  request.CheckInDate,
                EndDate =  request.CheckOutDate.AddDays(-1),
                NumberOfGuests = request.NumberOfGuests
            };
            
            var result = await hotelRoomsService.SearchAvailable(criteria);

            return result.Match<IActionResult>(
                success => Ok(success.HotelRooms.Select(r => new SearchAvailableResult
                {
                    HotelId = r.HotelId,
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    HotelRoomType = r.RoomType,
                })),
                _ => this.NotFoundProblem(
                    "Hotel Not Found",
                    $"Hotel with ID {hotelId} does not exist")
            );
        }
    }
}
