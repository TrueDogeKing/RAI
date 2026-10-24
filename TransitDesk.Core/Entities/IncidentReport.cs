using TransitDesk.Core.Enums;

namespace TransitDesk.Core.Entities;

/// <summary>Encja domenowa zgłoszenia awarii lub incydentu drogowego.</summary>
public class IncidentReport
{
    public int Id { get; set; }
    public string LineNumber { get; set; } = string.Empty;
    public int VehicleSideNumber { get; set; }
    public IncidentType IncidentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime ReportedAtUtc { get; set; }

    public string? AttachmentStoredFileName { get; set; }

    public string? AttachmentOriginalFileName { get; set; }
}
