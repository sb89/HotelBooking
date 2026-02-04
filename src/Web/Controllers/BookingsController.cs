using FluentValidation;
using Infrastructure.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Web.DTOs.Bookings;
using Web.Extensions;

namespace Web.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingsController(IBookingsService bookingsService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request,
            IValidator<CreateBookingRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return validationResult.ToValidationProblem(this);
            
            var result = await bookingsService.CreateBooking(request.CheckInDate, request.CheckOutDate.AddDays(-1), 
                request.RoomId, request.NumberOfGuests);
            
            return result.Match<IActionResult>(
                success => CreatedAtAction(
                    nameof(Get),
                    new { bookingReference = success.BookingId },
                    new { bookingId = success.BookingId }),
                _ => this.NotFoundProblem(
                    "Room Not Found",
                    "The specified room does not exist"),
                _ => this.UnprocessableEntityProblem(
                    "Capacity Exceeded",
                    "The number of guests exceeds the room's capacity"),
                _ => this.ConflictProblem(
                    "Room Unavailable",
                    "The room is no longer available for the selected dates")
            );
        }

        [HttpGet("{bookingReference:int}")]
        public async Task<IActionResult> Get(int bookingReference)
        {
            return Ok();
        }
    }
}
