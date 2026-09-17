namespace RestaurantOrderingPlatform.Application.Interfaces;

// Implemented in Infrastructure/Email using System.Net.Mail (SMTP).
// Fire-and-forget is NOT appropriate here (unlike WhatsApp notifications) —
// if sending fails, ForgotPassword should surface a 500, since the whole
// point of the request was to deliver that email.
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default);
}
