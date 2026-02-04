using FluentValidation;
using Infrastructure.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Web.DTOs.Bookings;
using Web.Extensions;

namespace Web.Controllers
{
    [Route("api/v1/bookings")]
    [ApiController]
    public class BookingsController(IBookingsService bookingsService) : ControllerBase
    {
        /// <summary>
        /// Create a new booking for a hotel room
        /// </summary>
        /// <param name="request">Booking details including room ID, dates, and number of guests</param>
        /// <param name="validator">Validator injected by framework</param>
        /// <returns>Booking reference number</returns>
        /// <response code="201">Booking created successfully</response>
        /// <response code="400">Invalid request (e.g., past dates, invalid guest count)</response>
        /// <response code="404">Room not found</response>
        /// <response code="409">Room already booked for selected dates</response>
        /// <response code="422">Number of guests exceeds room capacity</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request,
            IValidator<CreateBookingRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return validationResult.ToValidationProblem(this);
            
            var result = await bookingsService.CreateBooking(request.CheckInDate, request.CheckOutDate, 
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

        /// <summary>
        /// Get booking details by reference number
        /// </summary>
        /// <param name="bookingReference">The unique booking reference ID</param>
        /// <returns>Booking details including hotel name, room number, dates, and guest count</returns>
        /// <response code="200">Booking found and returned</response>
        /// <response code="404">Booking not found</response>
        [HttpGet("{bookingReference:int}")]
        [ProducesResponseType(typeof(GetBookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int bookingReference)
        {
            var booking = await bookingsService.Get(bookingReference);
            if (booking == null)
            {
                return this.NotFoundProblem("Booking Not Found", "The specified booking does not exist");
            }

            return Ok(new GetBookingResult
            {
                BookingReference =  bookingReference,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NoOfGuests,
                HotelName = booking.Room!.Hotel!.Name,
                RoomNumber = booking.Room!.RoomNumber
            });
        }
    }
}
