using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace ProjectDefense.Web.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = EmailTemplate.Wrap(subject, htmlMessage),
            IsBodyHtml = true,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(email);

        using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrEmpty(_options.Smtp.UserName))
        {
            client.Credentials = new NetworkCredential(_options.Smtp.UserName, _options.Smtp.Password);
        }

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("E-mail '{Subject}' sent to {Email} via SMTP {Host}:{Port}", subject, email, _options.Smtp.Host, _options.Smtp.Port);
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "Sending e-mail to {Email} failed", email);
            throw;
        }
    }
}
