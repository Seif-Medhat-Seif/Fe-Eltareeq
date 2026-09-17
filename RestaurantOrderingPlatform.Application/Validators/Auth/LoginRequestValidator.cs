using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Auth;

namespace RestaurantOrderingPlatform.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
