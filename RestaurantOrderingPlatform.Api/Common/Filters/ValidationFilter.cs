using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RestaurantOrderingPlatform.Api.Common.Filters;

// Registered globally in Program.cs. Replaces FluentValidation.AspNetCore's
// AddFluentValidationAutoValidation() — deprecated by FluentValidation's own
// maintainers and unable to run async validators (Module 1's email-uniqueness
// check needs one). See the Phase 6 "Correction (v3)" note. Controllers still
// never call .Validate() manually — it remains structurally impossible to
// forget validation on a new endpoint.
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator is null) continue; // no validator registered for this DTO — nothing to check

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(new
                {
                    statusCode = 400,
                    message = "Validation failed",
                    errors
                });
                return; // short-circuit — the action method never runs
            }
        }

        await next();
    }
}
