using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ProjectDefense.Web.Email;

public sealed class SendGridEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IOptions<EmailOptions> options, ILogger<SendGridEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.SendGrid.ApiKey))
        {
            throw new InvalidOperationException("SendGrid API key is not configured (Email:SendGrid:ApiKey).");
        }

        var client = new SendGridClient(_options.SendGrid.ApiKey);
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(_options.FromAddress, _options.FromName),
            new EmailAddress(email),
            subject,
            EmailTemplate.ToPlainText(htmlMessage),
            EmailTemplate.Wrap(subject, htmlMessage));
        message.SetClickTracking(false, false);

        var response = await client.SendEmailAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"SendGrid returned {(int)response.StatusCode}.");
        }
    }
}
