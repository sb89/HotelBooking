using FluentValidation;
using Web.DTOs.HotelRooms;

namespace Web.Validators.HotelRooms;

public class SearchAvailableRequestValidator : AbstractValidator<SearchAvailableRequest>
{
    public SearchAvailableRequestValidator()
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
    }
}