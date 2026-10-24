using System.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TransitDesk.Web.Models;

namespace TransitDesk.Web.Controllers;

public class HomeController(IOptions<RequestLocalizationOptions> localizationOptions) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Dispatch()
    {
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    // Zapisuje wybór języka w ciasteczku czytanym przez CookieRequestCultureProvider.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string? returnUrl)
    {
        var isSupported = localizationOptions.Value.SupportedUICultures?
            .Any(c => string.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (isSupported)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });
        }

        // LocalRedirect chroni przed Open Redirect (returnUrl spoza aplikacji → wyjątek).
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
