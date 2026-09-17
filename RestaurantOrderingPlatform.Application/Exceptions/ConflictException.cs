namespace RestaurantOrderingPlatform.Application.Exceptions;

// Mapped to HTTP 409 by Common/Middleware/ExceptionHandlingMiddleware (Api project).
// Used for business-state conflicts: "email already registered",
// "coupon already has usages", "already replied to this review", etc.
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
