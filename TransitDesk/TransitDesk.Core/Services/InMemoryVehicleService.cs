using TransitDesk.Core.Entities;
using TransitDesk.Core.Enums;

namespace TransitDesk.Core.Services;

/// <summary>
/// Ewidencja floty w pamięci. Rejestrowana jako Singleton, więc jest współdzielona
/// przez wszystkie żądania – stąd blokada chroniąca listę przed równoległym dostępem.
/// </summary>
public class InMemoryVehicleService : IVehicleService
{
    private readonly object _sync = new();
    private readonly List<Vehicle> _vehicles =
    [
        new() { SideNumber = 1001, ModelName = "Pesa Swing 120NaG", DepotName = "Wrzeszcz", TractionType = TractionType.Tram, ProductionYear = 2015, Capacity = 220, IsLowFloor = true, ServiceStatus = ServiceStatus.Active },
        new() { SideNumber = 1047, ModelName = "Bombardier NGT8", DepotName = "Nowy Port", TractionType = TractionType.Tram, ProductionYear = 2007, Capacity = 180, IsLowFloor = true, ServiceStatus = ServiceStatus.InMaintenance },
        new() { SideNumber = 1311, ModelName = "Konstal 105Na", DepotName = "Nowy Port", TractionType = TractionType.Tram, ProductionYear = 1988, Capacity = 125, IsLowFloor = false, ServiceStatus = ServiceStatus.Withdrawn },
        new() { SideNumber = 2072, ModelName = "Solaris Urbino 12", DepotName = "Hallera", TractionType = TractionType.Bus, ProductionYear = 2019, Capacity = 90, IsLowFloor = true, ServiceStatus = ServiceStatus.Active },
        new() { SideNumber = 2120, ModelName = "Mercedes-Benz Citaro G", DepotName = "Hallera", TractionType = TractionType.Bus, ProductionYear = 2012, Capacity = 150, IsLowFloor = true, ServiceStatus = ServiceStatus.Reserve },
        new() { SideNumber = 2205, ModelName = "Solaris Urbino 18 Electric", DepotName = "Wrzeszcz", TractionType = TractionType.Bus, ProductionYear = 2023, Capacity = 140, IsLowFloor = true, ServiceStatus = ServiceStatus.Active }
    ];

    public IReadOnlyList<Vehicle> GetAll()
    {
        lock (_sync)
        {
            return _vehicles.OrderBy(v => v.SideNumber).ToList();
        }
    }

    public IReadOnlyList<Vehicle> GetByDepot(string depotName, TractionType? tractionType)
    {
        lock (_sync)
        {
            return _vehicles
                .Where(v => string.Equals(v.DepotName, depotName, StringComparison.OrdinalIgnoreCase))
                .Where(v => tractionType is null || v.TractionType == tractionType)
                .OrderBy(v => v.SideNumber)
                .ToList();
        }
    }

    public IReadOnlyList<string> GetDepotNames()
    {
        lock (_sync)
        {
            return _vehicles.Select(v => v.DepotName).Distinct().Order().ToList();
        }
    }

    public Vehicle? Find(int sideNumber)
    {
        lock (_sync)
        {
            return _vehicles.FirstOrDefault(v => v.SideNumber == sideNumber);
        }
    }

    public bool IsActiveVehicle(int sideNumber)
    {
        var vehicle = Find(sideNumber);
        return vehicle is not null && vehicle.ServiceStatus != ServiceStatus.Withdrawn;
    }

    public bool TryAdd(Vehicle vehicle)
    {
        lock (_sync)
        {
            if (_vehicles.Any(v => v.SideNumber == vehicle.SideNumber))
            {
                return false;
            }

            vehicle.RegisteredAtUtc = DateTime.UtcNow;
            _vehicles.Add(vehicle);
            return true;
        }
    }

    public bool TryChangeStatus(int sideNumber, ServiceStatus newStatus)
    {
        lock (_sync)
        {
            var vehicle = _vehicles.FirstOrDefault(v => v.SideNumber == sideNumber);
            if (vehicle is null)
            {
                return false;
            }

            vehicle.ServiceStatus = newStatus;
            return true;
        }
    }
}
