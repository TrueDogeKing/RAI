using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace ProjectDefense.Web.Infrastructure;

/// <summary>Komunikaty UserManagera (rejestracja, zmiana hasła) w bieżącej kulturze.</summary>
public sealed class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public override IdentityError DefaultError() => Error(nameof(DefaultError), "IdentityDefaultError");

    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "IncorrectPassword");

    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "IdentityInvalidToken");

    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "IdentityInvalidUserName", userName ?? string.Empty);

    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), "IdentityInvalidEmail", email ?? string.Empty);

    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "IdentityDuplicateEmail", userName);

    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "IdentityDuplicateEmail", email);

    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), "IdentityPasswordTooShort", length);

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), "IdentityPasswordRequiresUniqueChars", uniqueChars);

    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "IdentityPasswordRequiresNonAlphanumeric");

    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "IdentityPasswordRequiresDigit");

    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "IdentityPasswordRequiresLower");

    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "IdentityPasswordRequiresUpper");

    private IdentityError Error(string code, string resourceKey, params object[] arguments)
        => new() { Code = code, Description = _localizer[resourceKey, arguments] };
}
