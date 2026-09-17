namespace RestaurantOrderingPlatform.Application.Exceptions;

// Mapped to HTTP 404.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Mapped to HTTP 401. Used for bad login credentials and invalid Google
// tokens — deliberately generic messages to avoid user enumeration.
public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
