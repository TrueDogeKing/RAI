using System.ComponentModel.DataAnnotations;
using TransitDesk.Core.Entities;
using TransitDesk.Core.Enums;

namespace TransitDesk.Web.ViewModels;

public class VehicleViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Podaj numer boczny pojazdu.")]
    [Range(1000, 9999, ErrorMessage = "Numer boczny musi mieścić się w zakresie 1000–9999.")]
    [Display(Name = "Numer boczny")]
    public int SideNumber { get; set; }

    [Required(ErrorMessage = "Podaj model pojazdu.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Nazwa modelu musi mieć od {2} do {1} znaków.")]
    [Display(Name = "Model pojazdu")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wybierz zajezdnię.")]
    [Display(Name = "Zajezdnia macierzysta")]
    public string DepotName { get; set; } = string.Empty;

    [Display(Name = "Rodzaj trakcji")]
    public TractionType TractionType { get; set; }

    [Range(1950, 2100, ErrorMessage = "Rok produkcji musi mieścić się w zakresie {1}–{2}.")]
    [Display(Name = "Rok produkcji")]
    public int ProductionYear { get; set; } = DateTime.Today.Year;

    [Range(10, 300, ErrorMessage = "Pojemność musi mieścić się w zakresie {1}–{2} pasażerów.")]
    [Display(Name = "Pojemność (pasażerów)")]
    public int Capacity { get; set; }

    [Display(Name = "Pojazd niskopodłogowy")]
    public bool IsLowFloor { get; set; }

    [Display(Name = "Status eksploatacyjny")]
    public ServiceStatus ServiceStatus { get; set; } = ServiceStatus.Active;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductionYear > DateTime.Today.Year)
        {
            yield return new ValidationResult(
                "Rok produkcji nie może być późniejszy niż bieżący.",
                [nameof(ProductionYear)]);
        }
    }

    public Vehicle ToEntity() => new()
    {
        SideNumber = SideNumber,
        ModelName = ModelName.Trim(),
        DepotName = DepotName,
        TractionType = TractionType,
        ProductionYear = ProductionYear,
        Capacity = Capacity,
        IsLowFloor = IsLowFloor,
        ServiceStatus = ServiceStatus
    };
}
