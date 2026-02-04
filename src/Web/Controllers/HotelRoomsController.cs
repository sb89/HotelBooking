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
        /// <summary>
        /// Find available rooms for a hotel within a date range
        /// </summary>
        /// <param name="hotelId">The hotel ID</param>
        /// <param name="request">Search criteria including check-in/out dates and number of guests</param>
        /// <param name="validator">Validator injected by framework</param>
        /// <returns>List of available rooms matching the criteria</returns>
        /// <response code="200">Available rooms found (may be empty list)</response>
        /// <response code="400">Invalid request (e.g., past dates, invalid guest count)</response>
        /// <response code="404">Hotel not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SearchAvailableResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchAvailable(int hotelId, [FromQuery]SearchAvailableRequest request,
            IValidator<SearchAvailableRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return validationResult.ToValidationProblem(this);
            
            var criteria = new SearchAvailableCriteria
            {
                HotelId =  hotelId,
                CheckInDate =  request.CheckInDate,
                CheckoutDate =  request.CheckOutDate,
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
