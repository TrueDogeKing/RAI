using Microsoft.AspNetCore.Localization;

namespace ProjectDefense.Web.Infrastructure;

public static class CultureEndpoints
{
    public static readonly string[] SupportedCultures = ["pl-PL", "en-US"];

    public static IEndpointRouteBuilder MapCultureSwitch(this IEndpointRouteBuilder app)
    {
        app.MapGet("/culture/{culture}", (string culture, string? returnUrl, HttpContext context) =>
        {
            if (!SupportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest();
            }

            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });

            var target = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
                ? returnUrl
                : "/";
            return Results.LocalRedirect(target);
        }).ExcludeFromDescription();

        return app;
    }
}
