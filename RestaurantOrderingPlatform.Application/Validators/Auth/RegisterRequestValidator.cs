using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Auth;

namespace RestaurantOrderingPlatform.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.AccountType)
            .IsInEnum();

        // Email uniqueness is NOT checked here — it requires an async DB
        // lookup, which is a Service-layer concern per the Phase 6 rule:
        // "Validators handle shape only; business rules requiring a DB
        // lookup belong in the Service." AuthService checks it and returns
        // a specific 409 Conflict, not a generic validation error.
    }
}
