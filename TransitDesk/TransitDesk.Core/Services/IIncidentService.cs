using TransitDesk.Core.Entities;

namespace TransitDesk.Core.Services;

public interface IIncidentService
{
    Task<IncidentReport> RegisterIncidentAsync(IncidentReport report, CancellationToken cancellationToken = default);
    IReadOnlyList<IncidentReport> GetAll();
    IncidentReport? Find(int id);
}
