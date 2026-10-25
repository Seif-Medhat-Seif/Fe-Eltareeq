using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Coupons;

namespace RestaurantOrderingPlatform.Application.Validators.Coupons;

public class CreateCouponRequestValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponRequestValidator()
    {
        Include(new CouponTermsValidator<CreateCouponRequest>());

        // بنقبل حروف صغيرة (الـ Service بيحوّلها uppercase). ممنوع مسافات ورموز عشان العميل يكتبه من غير لبس.
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^[A-Za-z0-9_-]{3,30}$")
            .WithMessage("Code must be 3-30 characters: letters, digits, '-' or '_'.");

        // كوبون بتاريخ انتهاء فات مالوش معنى وقت الإنشاء. (في Update مفيش القاعدة دي: تعديل كوبون منتهي مسموح.)
        RuleFor(x => x.ExpiryDate!.Value)
            .Must(d => UtcDateTime.Normalize(d) > DateTime.UtcNow)
            .WithMessage("ExpiryDate must be in the future.")
            .When(x => x.ExpiryDate.HasValue)
            .OverridePropertyName(nameof(CreateCouponRequest.ExpiryDate));
    }
}
