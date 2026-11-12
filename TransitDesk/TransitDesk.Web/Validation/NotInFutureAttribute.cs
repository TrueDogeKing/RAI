using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TransitDesk.Web.Validation;

/// <summary>
/// Data nie może leżeć w przyszłości. Walidacja hybrydowa:
/// serwer – <see cref="IsValid"/>, klient – atrybuty data-val-notinfuture* obsługiwane
/// przez adapter jQuery Unobtrusive w wwwroot/js/validation-notinfuture.js.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotInFutureAttribute : ValidationAttribute, IClientModelValidator
{
    /// <summary>Tolerancja na rozjazd zegarów klienta i serwera.</summary>
    public const int ClockSkewMinutes = 5;

    public NotInFutureAttribute()
        : base("Pole {0} nie może zawierać daty z przyszłości.")
    {
    }

    public override bool IsValid(object? value) => value switch
    {
        null => true, // brak wartości to zadanie dla [Required]
        DateTime dateTime => dateTime <= DateTime.Now.AddMinutes(ClockSkewMinutes),
        DateTimeOffset offset => offset <= DateTimeOffset.Now.AddMinutes(ClockSkewMinutes),
        _ => false
    };

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-notinfuture",
            FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        MergeAttribute(context.Attributes, "data-val-notinfuture-skew",
            ClockSkewMinutes.ToString());
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        attributes.TryAdd(key, value);
    }
}
