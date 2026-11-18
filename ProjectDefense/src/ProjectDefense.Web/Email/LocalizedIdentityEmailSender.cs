using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Localization;

namespace ProjectDefense.Web.Email;

/// <summary>
/// Dekorator IEmailSender: strony Identity wysyłają angielskie wiadomości („Confirm your email”, „Reset Password”).
/// Rozpoznajemy je po temacie, wyciągamy link i składamy treść w bieżącej kulturze. Pozostałe e-maile przechodzą bez zmian.
/// </summary>
public sealed partial class LocalizedIdentityEmailSender : IEmailSender
{
    private readonly IEmailSender _inner;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedIdentityEmailSender(IEmailSender inner, IStringLocalizer<SharedResource> localizer)
    {
        _inner = inner;
        _localizer = localizer;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var link = LinkRegex().Match(htmlMessage);
        if (link.Success)
        {
            // Href jest już zakodowany przez stronę Identity (HtmlEncoder) – przenosimy go bez zmian.
            var href = link.Groups["href"].Value;
            switch (subject)
            {
                case "Confirm your email":
                    return _inner.SendEmailAsync(email, _localizer["ConfirmEmailSubject"], _localizer["ConfirmEmailBody", href]);
                case "Reset Password":
                case "Reset your password":
                    return _inner.SendEmailAsync(email, _localizer["ResetPasswordSubject"], _localizer["ResetPasswordBody", href]);
            }
        }

        return _inner.SendEmailAsync(email, subject, htmlMessage);
    }

    [GeneratedRegex("href=['\"](?<href>[^'\"]+)['\"]")]
    private static partial Regex LinkRegex();
}
