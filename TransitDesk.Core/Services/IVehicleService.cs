using TransitDesk.Core.Entities;
using TransitDesk.Core.Enums;

namespace TransitDesk.Core.Services;

public interface IVehicleService
{
    IReadOnlyList<Vehicle> GetAll();
    IReadOnlyList<Vehicle> GetByDepot(string depotName, TractionType? tractionType);
    IReadOnlyList<string> GetDepotNames();
    Vehicle? Find(int sideNumber);

    /// <summary>Pojazd istnieje w ewidencji i nie został wycofany z eksploatacji.</summary>
    bool IsActiveVehicle(int sideNumber);

    /// <summary>Dodaje pojazd; zwraca false, jeśli numer boczny jest już zajęty.</summary>
    bool TryAdd(Vehicle vehicle);

    bool TryChangeStatus(int sideNumber, ServiceStatus newStatus);
}
