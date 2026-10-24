using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TransitDesk.Web.Controllers;

/// <summary>
/// Cel re-ekzekucji z UseStatusCodePagesWithReExecute("/Errors/Status", "?code={0}").
/// </summary>
public class ErrorsController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int code)
    {
        if (code is >= 400 and <= 599)
        {
            Response.StatusCode = code;
        }

        var reExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        ViewBag.StatusCode = code;
        ViewBag.OriginalPath = reExecuteFeature?.OriginalPath;
        ViewBag.Title = code switch
        {
            404 => "Brak zasobu",
            403 => "Odmowa dostępu",
            _ => "Błąd protokołu HTTP"
        };
        ViewBag.Message = code switch
        {
            404 => "Nie odnaleziono wskazanego zasobu taboru lub zgłoszenia.",
            403 => "Brak uprawnień dostępu do modułu dyspozytorskiego.",
            _ => "Wystąpił nieoczekiwany błąd protokołu HTTP."
        };

        return View();
    }
}
