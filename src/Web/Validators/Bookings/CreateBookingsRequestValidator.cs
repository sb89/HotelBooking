using FluentValidation;
using Web.DTOs.Bookings;

namespace Web.Validators.Bookings;

public class CreateBookingsRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingsRequestValidator()
    {
        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Check In Date must be today or in the future");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Check Out Date must be after Check In Date");
        
        RuleFor(x => x.NumberOfGuests)
            .InclusiveBetween(1, 25)
            .WithMessage("Number of guests must be between 1 and 25");
        
        RuleFor(x => x.RoomId)
            .GreaterThanOrEqualTo(1)
            .WithMessage("RoomId must be greater than 0");
    }
}