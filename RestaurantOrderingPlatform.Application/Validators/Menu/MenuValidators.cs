using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Menu;

namespace RestaurantOrderingPlatform.Application.Validators.Menu;

public class CreateMenuCategoryRequestValidator : AbstractValidator<CreateMenuCategoryRequest>
{
    public CreateMenuCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateMenuCategoryRequestValidator : AbstractValidator<UpdateMenuCategoryRequest>
{
    public UpdateMenuCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateMenuItemRequestValidator : AbstractValidator<CreateMenuItemRequest>
{
    public CreateMenuItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public class UpdateMenuItemRequestValidator : AbstractValidator<UpdateMenuItemRequest>
{
    public UpdateMenuItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public class SetMenuItemAvailabilityRequestValidator : AbstractValidator<SetMenuItemAvailabilityRequest>
{
    public SetMenuItemAvailabilityRequestValidator()
    {
        RuleFor(x => x.IsAvailable).NotNull();
    }
}
