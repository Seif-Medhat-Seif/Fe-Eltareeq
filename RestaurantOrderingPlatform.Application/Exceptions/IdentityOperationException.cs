namespace RestaurantOrderingPlatform.Application.Exceptions;

// Thrown by IIdentityService implementations. Deliberately a plain
// exception with a string list, not Microsoft.AspNetCore.Identity.IdentityResult
// or IdentityError — the Application layer must never reference Identity types.
public class IdentityOperationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public IdentityOperationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
