using Microsoft.AspNetCore.Mvc;
using TransitDesk.Core.Services;
using TransitDesk.Web.Filters;
using TransitDesk.Web.Security;
using TransitDesk.Web.ViewModels;

namespace TransitDesk.Web.Controllers;

public class IncidentsController(
    IIncidentService incidentService,
    IVehicleService vehicleService,
    IWebHostEnvironment environment,
    ILogger<IncidentsController> logger) : Controller
{
    private const string AttachmentFieldName = "attachmentFile";

    // Poza wwwroot – pliki nie są serwowane przez UseStaticFiles, dostęp tylko przez akcję Attachment.
    private string UploadsDirectory => Path.Combine(environment.ContentRootPath, "App_Data", "Uploads");

    public IActionResult Index()
    {
        return View(incidentService.GetAll());
    }

    [HttpGet]
    public IActionResult Report()
    {
        return View(new IncidentReportViewModel { OccurredAt = TruncateToMinutes(DateTime.Now) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditLog]
    // Kestrel odrzuci większe ciało żądania, zanim trafi do pamięci/dysku (ochrona przed DoS).
    [RequestSizeLimit(UploadedFileValidator.MaxFileSizeBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadedFileValidator.MaxFileSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> Report(IncidentReportViewModel model, IFormFile? attachmentFile, CancellationToken cancellationToken)
    {
        // [Remote] działa tylko w przeglądarce – serwer MUSI powtórzyć tę samą regułę.
        if (model.VehicleSideNumber is int sideNumber && !vehicleService.IsActiveVehicle(sideNumber))
        {
            ModelState.AddModelError(nameof(model.VehicleSideNumber), "Pojazd o podanym numerze nie istnieje lub jest wycofany.");
        }

        if (attachmentFile is { Length: > 0 })
        {
            var fileError = await UploadedFileValidator.ValidateAsync(attachmentFile, cancellationToken);
            if (fileError is not null)
            {
                ModelState.AddModelError(AttachmentFieldName, fileError);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var incident = model.ToEntity();

        if (attachmentFile is { Length: > 0 })
        {
            // Nazwa klienta (np. "..\\..\\web.config.pdf") nigdy nie trafia do ścieżki – tylko zweryfikowane rozszerzenie.
            var extension = Path.GetExtension(attachmentFile.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid()}{extension}";

            Directory.CreateDirectory(UploadsDirectory);
            var targetPath = Path.Combine(UploadsDirectory, storedFileName);

            // FileMode.CreateNew – nigdy nie nadpisuje istniejącego pliku; useAsync – prawdziwe asynchroniczne I/O.
            await using (var fileStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             bufferSize: 81920, useAsync: true))
            {
                await attachmentFile.CopyToAsync(fileStream, cancellationToken);
            }

            incident.AttachmentStoredFileName = storedFileName;
            incident.AttachmentOriginalFileName = Path.GetFileName(attachmentFile.FileName);
            logger.LogInformation("Zapisano załącznik {StoredFileName} ({Length} B)", storedFileName, attachmentFile.Length);
        }

        await incidentService.RegisterIncidentAsync(incident, cancellationToken);

        TempData["SuccessMessage"] = $"Zgłoszenie #{incident.Id} (linia {incident.LineNumber}, pojazd #{incident.VehicleSideNumber}) zostało zarejestrowane.";
        return RedirectToAction(nameof(Index));
    }

    // Wywoływane AJAX-em przez jquery.validate.unobtrusive dla pola z [Remote]: GET /Incidents/VerifyVehicleAvailable?VehicleSideNumber=2072
    [AcceptVerbs("GET", "POST")]
    public IActionResult VerifyVehicleAvailable(int vehicleSideNumber)
    {
        if (!vehicleService.IsActiveVehicle(vehicleSideNumber))
        {
            // Zwrócenie łańcucha = błąd walidacji z tą treścią; Json(true) = poprawne.
            return Json($"Pojazd #{vehicleSideNumber} nie występuje w czynnym rejestrze floty.");
        }

        return Json(true);
    }

    // Kontrolowany dostęp do załącznika – identyfikujemy go po Id zgłoszenia, nie po ścieżce od klienta.
    [HttpGet]
    public IActionResult Attachment(int id)
    {
        var incident = incidentService.Find(id);
        if (incident?.AttachmentStoredFileName is null)
        {
            return NotFound();
        }

        var path = Path.Combine(UploadsDirectory, incident.AttachmentStoredFileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var contentType = UploadedFileValidator.GetContentType(Path.GetExtension(path));
        return PhysicalFile(path, contentType, incident.AttachmentStoredFileName);
    }

    private static DateTime TruncateToMinutes(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
}
