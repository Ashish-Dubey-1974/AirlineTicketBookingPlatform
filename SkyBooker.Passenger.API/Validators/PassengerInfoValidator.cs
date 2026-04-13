using FluentValidation;
using SkyBooker.Passenger.API.DTOs;
using SkyBooker.Passenger.API.Entities;

namespace SkyBooker.Passenger.API.Validators;

public class PassengerInfoValidator : AbstractValidator<PassengerRequestDto>
{
    public PassengerInfoValidator()
    {
        RuleFor(p => p.Title)
            .NotEmpty()
            .MaximumLength(10)
            .Must(t => new[] { "Mr", "Mrs", "Ms", "Dr", "Master", "Miss" }.Contains(t))
            .WithMessage("Title must be Mr, Mrs, Ms, Dr, Master, or Miss");

        RuleFor(p => p.FirstName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-zA-Z\\s'-]+$")
            .WithMessage("First name can only contain letters, spaces, apostrophes, and hyphens");

        RuleFor(p => p.LastName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-zA-Z\\s'-]+$")
            .WithMessage("Last name can only contain letters, spaces, apostrophes, and hyphens");

        RuleFor(p => p.DateOfBirth)
            .NotEmpty()
            .Must(BeValidAge)
            .WithMessage("Age does not match passenger type (ADULT 12+, CHILD 2-11, INFANT <2)");

        RuleFor(p => p.Gender)
            .NotEmpty()
            .Must(g => new[] { "Male", "Female", "Other", "Prefer not to say" }.Contains(g))
            .WithMessage("Gender must be Male, Female, Other, or Prefer not to say");

        RuleFor(p => p.PassportNumber)
            .NotEmpty()
            .Length(6, 20)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Passport number must contain only uppercase letters and numbers");

        RuleFor(p => p.Nationality)
            .NotEmpty()
            .MaximumLength(60);

        RuleFor(p => p.PassportExpiry)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Passport must not be expired");

        RuleFor(p => p.PassengerType)
            .NotEmpty()
            .Must(t => new[] { "ADULT", "CHILD", "INFANT" }.Contains(t))
            .WithMessage("PassengerType must be ADULT, CHILD, or INFANT");
    }

    private bool BeValidAge(PassengerRequestDto dto, DateTime dateOfBirth)
    {
        var age = CalculateAge(dateOfBirth);
        
        return dto.PassengerType switch
        {
            PassengerTypeConstants.Adult => age >= 12,
            PassengerTypeConstants.Child => age >= 2 && age < 12,
            PassengerTypeConstants.Infant => age < 2,
            _ => false
        };
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}