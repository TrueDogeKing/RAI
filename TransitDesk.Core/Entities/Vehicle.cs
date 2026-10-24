using TransitDesk.Core.Enums;

namespace TransitDesk.Core.Entities;

public class Vehicle
{
    public int SideNumber { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string DepotName { get; set; } = string.Empty;
    public TractionType TractionType { get; set; }
    public int ProductionYear { get; set; }
    public int Capacity { get; set; }
    public bool IsLowFloor { get; set; }
    public ServiceStatus ServiceStatus { get; set; }
    public DateTime RegisteredAtUtc { get; set; }
}
