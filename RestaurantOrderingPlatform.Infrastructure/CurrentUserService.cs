using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    public Guid? UserId =>
        Guid.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Role => _http.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => _http.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}