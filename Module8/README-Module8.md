# Module 8 — Coupons

## 1) مكان الملفات (انسخها بنفس المسارات)
| الملف | المشروع |
|---|---|
| Application/Common/UtcDateTime.cs | Application |
| Application/Common/Interfaces/ICouponService.cs | Application |
| Application/DTOs/Coupons/*.cs (5 ملفات) | Application |
| Application/Validators/Coupons/*.cs (3 ملفات) | Application |
| Application/Services/CouponService.cs | Application |
| Api/Controllers/CouponsController.cs | Api |

> `CouponService` بيستخدم `Application.Common.Exceptions` (ConflictException / NotFoundException / UnauthorizedAppException). لو الـ namespace عندك مختلف عدّل الـ using بس.

## 2) DI (Program.cs)
```csharp
builder.Services.AddScoped<ICouponService, CouponService>();
```
الـ validators بتتسجل لوحدها لو عندك `AddValidatorsFromAssembly`.

## 3) تعديلات على ملفات موجودة (أسطر بس)
**Infrastructure/Persistence/Configurations/CouponConfiguration.cs** — اتأكد إن فيه:
```csharp
builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
builder.Property(c => c.DiscountType).HasConversion<string>().HasMaxLength(20);
builder.HasIndex(c => new { c.RestaurantId, c.Code }).IsUnique();
builder.HasMany<CouponUsage>().WithOne(u => u.Coupon).HasForeignKey(u => u.CouponId).OnDelete(DeleteBehavior.Restrict);
```
**CouponUsageConfiguration**: `HasIndex(u => u.OrderId).IsUnique()` (موجود من Module 6) + index على `(CouponId, CustomerId)` لسرعة فحص الـ per-customer limit.

لو الـ Domain عندك مفيهوش `DbSet<Cart>.CouponId` nullable، اتأكد إن `Cart.CouponId` من نوع `Guid?`.

## 4) Migration (Package Manager Console)
```
Add-Migration Module8_CouponIndexes -Project Infrastructure -StartupProject Api
Update-Database -Project Infrastructure -StartupProject Api
```
لو مفيش تغيير في الـ model هيطلع Migration فاضي، امسحه بـ `Remove-Migration`.

## 5) التحديات والقرارات
- **تكرار الكود (race)**: pre-check برسالة 409 واضحة، لكن الحَكَم الـ UNIQUE (RestaurantId, Code). لو طلبين متزامنين، التاني بيقع في DbUpdateException ونحوّله لـ 409.
- **الحذف vs الاستخدام (race)**: الـ check قبل الحذف مش كفاية (webhook ممكن يسجّل CouponUsage بعده). الـ FK Restrict بيرفض الحذف ونرجّع 409. وبنمنع الحذف كمان لو في Order (حتى PendingPayment) شايل الكوبون.
- **السلات**: الحذف بيشيل الكوبون من أي Cart مطبّقاه (ExecuteUpdate) في نفس الـ transaction.
- **التعديل**: الأوردرات القديمة مش بتتأثر (snapshot في Order.DiscountAmount). قرار مني: الـ Code ثابت (مش في UpdateRequest)، و`DiscountType` مقفول بعد أول استخدام، و`UsageLimitTotal` ما ينفعش يقل عن عدد الاستخدامات الحالي (409).
- **كوبون اتعدّل بين الـ cart و الـ checkout**: الـ checkout (Module 6) بيعيد التحقق دايمًا، فالقيم الجديدة هي اللي بتتطبق. مفيش تخزين للخصم في الـ cart.
- **التواريخ**: `UtcDateTime.Normalize` بيعتبر أي تاريخ من غير Z هو UTC، عشان نتفادى اختلاف توقيت السيرفر.
- **الـ Code**: Trim + Upper، حروف/أرقام/`-`/`_`، من 3 لـ 30.
- **PATCH active**: UPDATE واحد idempotent.
- **Validation**: MaxDiscountAmount مع FixedAmount = 400، الـ Percentage ≤ 100، الكسور لحد رقمين، ExpiryDate لازم في المستقبل عند الإنشاء بس.

## 6) محتاج تأكيدك
1. صاحب المطعم عنده مطعم واحد (V1)؟ لو أكتر محتاجين `restaurantId` في الـ route.
2. الكوبون المنتهي يفضل ظاهر في الـ list (ايوه، مع `IsExpired=true`).

## 7) Jira
- ROP-81 CouponService + CRUD endpoints
- ROP-82 Validators (Create/Update)
- ROP-83 Duplicate code 409 + unique-index race handling
- ROP-84 Delete rules (usage/order block + cart cleanup)
- ROP-85 Edit-lock rules after first use
- ROP-86 Coupon indexes migration
- ROP-87 Tests: duplicate race, delete-vs-webhook race, edit rules
