using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        var smtpSection = _configuration.GetSection("Smtp");

        using var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]!))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"])
        };

        using var message = new MailMessage
        {
            From = new MailAddress(smtpSection["FromAddress"]!, smtpSection["FromName"] ?? "Restaurant Ordering Platform"),
            Subject = "Reset your password",
            IsBodyHtml = true,
            Body = $@"
                <p>Hi {toName},</p>
                <p>We received a request to reset your password. Click the link below to choose a new one:</p>
                <p><a href=""{resetLink}"">Reset Password</a></p>
                <p>If you didn't request this, you can safely ignore this email.</p>"
        };

        message.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(message, ct);
    }
}
