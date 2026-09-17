using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Auth;

namespace RestaurantOrderingPlatform.Application.Validators.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
