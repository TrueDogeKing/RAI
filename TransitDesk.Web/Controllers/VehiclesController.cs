using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TransitDesk.Core.Enums;
using TransitDesk.Core.Services;
using TransitDesk.Web.Filters;
using TransitDesk.Web.Infrastructure;
using TransitDesk.Web.Models;
using TransitDesk.Web.ViewModels;

namespace TransitDesk.Web.Controllers;

public class VehiclesController(IVehicleService vehicleService) : Controller
{
    private static readonly string[] KnownDepots = ["Hallera", "Nowy Port", "Wrzeszcz"];

    // GET /Vehicles
    public IActionResult Index()
    {
        return View(vehicleService.GetAll());
    }

    // GET /Zajezdnia/{depotName}/{tractionType?} – trasa konwencjonalna "depot_fleet" z Program.cs
    public IActionResult DepotFleetList(string depotName, string? tractionType)
    {
        TractionType? traction = null;
        if (!string.IsNullOrEmpty(tractionType))
        {
            traction = ParseTraction(tractionType);
            if (traction is null)
            {
                return NotFound(); // → 404 obsłużone przez ErrorsController
            }
        }

        var vehicles = vehicleService.GetByDepot(depotName, traction);
        if (vehicles.Count == 0 && !vehicleService.GetDepotNames().Contains(depotName, StringComparer.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return View(new DepotFleetViewModel { DepotName = depotName, TractionType = traction, Vehicles = vehicles });
    }

    // GET /pojazd/2072 – routing atrybutowy z własnym ograniczeniem; /pojazd/999 nie dopasuje się w ogóle (404).
    [HttpGet("pojazd/{sideNumber:fleetSideNo}")]
    public IActionResult Details(int sideNumber)
    {
        var vehicle = vehicleService.Find(sideNumber);
        if (vehicle is null)
        {
            return NotFound();
        }

        ViewBag.StatusOptions = new SelectList(
            Enum.GetValues<ServiceStatus>().Select(s => new { Value = s, Text = s.GetDisplayName() }),
            "Value", "Text", vehicle.ServiceStatus);
        return View(vehicle);
    }

    [HttpGet]
    public IActionResult Create()
    {
        PopulateDepots();
        return View(new VehicleViewModel());
    }

    // Post-Redirect-Get: błąd → ten sam widok z modelem; sukces → TempData + 302 na Index.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditLog]
    public IActionResult Create(VehicleViewModel model)
    {
        if (!KnownDepots.Contains(model.DepotName))
        {
            ModelState.AddModelError(nameof(model.DepotName), "Wybierz zajezdnię z listy.");
        }

        if (!ModelState.IsValid)
        {
            PopulateDepots();
            return View(model);
        }

        if (!vehicleService.TryAdd(model.ToEntity()))
        {
            // Klucz "" = błąd na poziomie modelu → wyświetli go asp-validation-summary="ModelOnly".
            ModelState.AddModelError(string.Empty, $"Pojazd o numerze bocznym {model.SideNumber} już figuruje w ewidencji floty.");
            PopulateDepots();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Pojazd #{model.SideNumber} ({model.ModelName}) został dodany do ewidencji.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditLog]
    public IActionResult ChangeStatus(int sideNumber, ServiceStatus serviceStatus)
    {
        if (!Enum.IsDefined(serviceStatus) || !vehicleService.TryChangeStatus(sideNumber, serviceStatus))
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = $"Zmieniono status pojazdu #{sideNumber} na: {serviceStatus.GetDisplayName()}.";
        return RedirectToAction(nameof(Details), new { sideNumber });
    }

    // ───────────── Lista monitorowanych pojazdów (stan w sesji) ─────────────

    [HttpGet]
    public IActionResult Watchlist()
    {
        return View(HttpContext.Session.GetObject<List<WatchedVehicle>>(SessionKeys.Watchlist) ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddVehicleToWatchlist(int sideNumber, string? returnUrl)
    {
        var vehicle = vehicleService.Find(sideNumber);
        if (vehicle is null)
        {
            return NotFound();
        }

        // Odczyt → modyfikacja → zapis: sesja przechowuje kopię (JSON), nie referencję do obiektu.
        var watchlist = HttpContext.Session.GetObject<List<WatchedVehicle>>(SessionKeys.Watchlist) ?? [];
        if (watchlist.All(w => w.SideNumber != sideNumber))
        {
            watchlist.Add(new WatchedVehicle
            {
                SideNumber = vehicle.SideNumber,
                ModelName = vehicle.ModelName,
                DepotName = vehicle.DepotName,
                AddedAtUtc = DateTime.UtcNow
            });
            HttpContext.Session.SetObject(SessionKeys.Watchlist, watchlist);
            TempData["SuccessMessage"] = $"Pojazd #{sideNumber} dodano do listy monitorowanych.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Pojazd #{sideNumber} jest już monitorowany.";
        }

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Watchlist));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveVehicleFromWatchlist(int sideNumber)
    {
        var watchlist = HttpContext.Session.GetObject<List<WatchedVehicle>>(SessionKeys.Watchlist) ?? [];
        watchlist.RemoveAll(w => w.SideNumber == sideNumber);
        HttpContext.Session.SetObject(SessionKeys.Watchlist, watchlist);
        return RedirectToAction(nameof(Watchlist));
    }

    private void PopulateDepots()
    {
        ViewBag.Depots = new SelectList(KnownDepots);
    }

    private static TractionType? ParseTraction(string value) => value.ToLowerInvariant() switch
    {
        "tramwaj" or "tramwaje" or "tram" => TractionType.Tram,
        "autobus" or "autobusy" or "bus" => TractionType.Bus,
        _ => null
    };
}
