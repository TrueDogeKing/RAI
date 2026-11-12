using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TransitDesk.Core.Entities;
using TransitDesk.Core.Enums;
using TransitDesk.Web.Validation;

namespace TransitDesk.Web.ViewModels;

/// <summary>Model formularza zgłoszenia incydentu (walidacja klient + serwer + zdalna).</summary>
public class IncidentReportViewModel
{
    [Required(ErrorMessage = "Podaj numer linii.")]
    [RegularExpression("^[A-Za-z0-9]{1,4}$", ErrorMessage = "Numer linii to od 1 do 4 znaków alfanumerycznych (np. 6, 199, N1).")]
    [Display(Name = "Numer linii")]
    public string LineNumber { get; set; } = string.Empty;

    // int? zamiast int: puste pole daje null → komunikat z [Required], a nie błąd konwersji.
    [Required(ErrorMessage = "Podaj numer boczny pojazdu.")]
    [Range(1000, 9999, ErrorMessage = "Numer boczny musi mieścić się w zakresie 1000–9999.")]
    [Remote(action: "VerifyVehicleAvailable", controller: "Incidents",
        ErrorMessage = "Pojazd o podanym numerze nie istnieje lub jest wycofany.")]
    [Display(Name = "Numer boczny pojazdu")]
    public int? VehicleSideNumber { get; set; }

    [Display(Name = "Rodzaj zdarzenia")]
    public IncidentType IncidentType { get; set; }

    [Required(ErrorMessage = "Opisz zdarzenie.")]
    [StringLength(500, MinimumLength = 20, ErrorMessage = "Opis musi mieć od {2} do {1} znaków.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Opis zdarzenia")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj datę i godzinę zdarzenia.")]
    [DataType(DataType.DateTime)]
    [NotInFuture(ErrorMessage = "Data zdarzenia nie może być datą z przyszłości.")]
    [Display(Name = "Data i godzina zdarzenia")]
    public DateTime? OccurredAt { get; set; }

    public IncidentReport ToEntity() => new()
    {
        LineNumber = LineNumber.ToUpperInvariant(),
        VehicleSideNumber = VehicleSideNumber!.Value,
        IncidentType = IncidentType,
        Description = Description.Trim(),
        OccurredAt = OccurredAt!.Value
    };
}
