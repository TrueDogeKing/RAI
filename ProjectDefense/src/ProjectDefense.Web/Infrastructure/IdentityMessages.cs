using Microsoft.Extensions.Localization;

namespace ProjectDefense.Web.Infrastructure;

/// <summary>
/// Komunikaty ustawiane w kodzie stron Identity (ModelState, StatusMessage) są zapisane na sztywno po angielsku.
/// Widoki tłumaczą je przy wyświetlaniu – po dokładnej treści angielskiej mapowanej na klucz zasobu.
/// </summary>
public static class IdentityMessages
{
    private static readonly Dictionary<string, string> Keys = new(StringComparer.Ordinal)
    {
        ["Invalid login attempt."] = "InvalidLoginAttempt",
        ["Verification email sent. Please check your email."] = "VerificationEmailSent",
        ["Thank you for confirming your email."] = "EmailConfirmed",
        ["Error confirming your email."] = "EmailConfirmError",
        ["Thank you for confirming your email change."] = "EmailChangeConfirmed",
        ["Error changing email."] = "EmailChangeError",
        ["Error changing user name."] = "UserNameChangeError",
        ["Your profile has been updated"] = "ProfileUpdated",
        ["Unexpected error when trying to set phone number."] = "PhoneUpdateError",
        ["Confirmation link to change email sent. Please check your email."] = "EmailChangeLinkSent",
        ["Your email is unchanged."] = "EmailUnchanged",
        ["Your password has been changed."] = "PasswordChanged",
        ["Your password has been set."] = "PasswordChanged",
        ["Incorrect password."] = "IncorrectPassword"
    };

    public static string Translate(this IStringLocalizer localizer, string message)
        => Keys.TryGetValue(message, out var key) ? localizer[key] : message;
}
