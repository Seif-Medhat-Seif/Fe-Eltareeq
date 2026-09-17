using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Auth;

namespace RestaurantOrderingPlatform.Application.Validators.Auth;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
        RuleFor(x => x.AccountType).IsInEnum();
    }
}
