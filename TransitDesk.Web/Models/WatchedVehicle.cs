namespace TransitDesk.Web.Models;

public class WatchedVehicle
{
    public int SideNumber { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string DepotName { get; set; } = string.Empty;
    public DateTime AddedAtUtc { get; set; }
}
