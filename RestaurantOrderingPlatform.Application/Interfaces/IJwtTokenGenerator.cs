namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IJwtTokenGenerator
{
    // Multiple roles supported per Module 6 of the earlier plan (the
    // POST /api/auth/roles feature), even though V1 registration only
    // ever assigns one role at creation time.
    (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email, IReadOnlyList<string> roles);
}
