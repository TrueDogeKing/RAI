using TransitDesk.Core.Entities;
using TransitDesk.Core.Enums;

namespace TransitDesk.Web.ViewModels;

public class DepotFleetViewModel
{
    public required string DepotName { get; init; }
    public TractionType? TractionType { get; init; }
    public required IReadOnlyList<Vehicle> Vehicles { get; init; }
}
