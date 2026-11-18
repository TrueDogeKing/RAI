using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;

namespace ProjectDefense.Web.Infrastructure;

/// <summary>
/// Formularze stron Identity (InputModel z pakietu Microsoft.AspNetCore.Identity.UI) mają angielskie
/// etykiety i atrybuty walidacji bez ErrorMessage, więc AddDataAnnotationsLocalization ich nie tłumaczy.
/// Provider podstawia klucze z SharedResource: etykieta = nazwa właściwości, komunikat = klucz walidacji.
/// </summary>
public sealed class IdentityUiLocalizationMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private static readonly System.Reflection.Assembly IdentityUiAssembly = typeof(UIFrameworkAttribute).Assembly;

    private readonly IStringLocalizer _localizer;

    public IdentityUiLocalizationMetadataProvider(IStringLocalizerFactory localizerFactory)
    {
        _localizer = localizerFactory.Create(typeof(SharedResource));
    }

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (!IsIdentityUiProperty(context.Key))
        {
            return;
        }

        var key = context.Key.Name!;
        context.DisplayMetadata.DisplayName = () => _localizer[key];
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (!IsIdentityUiProperty(context.Key))
        {
            return;
        }

        foreach (var attribute in context.Attributes.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessageResourceName is not null)
            {
                continue;
            }

            // Komunikat staje się kluczem zasobu; DataAnnotationsLocalization wstawi pod {0} przetłumaczoną etykietę.
            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "Required",
                EmailAddressAttribute => "InvalidEmail",
                PhoneAttribute => "InvalidPhone",
                StringLengthAttribute => "PasswordLength",
                CompareAttribute => "PasswordMismatch",
                _ => attribute.ErrorMessage
            };
        }
    }

    private static bool IsIdentityUiProperty(ModelMetadataIdentity key)
        => key.MetadataKind == ModelMetadataKind.Property
            && key.ContainerType?.Assembly == IdentityUiAssembly;
}
