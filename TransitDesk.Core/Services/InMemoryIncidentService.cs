using TransitDesk.Core.Entities;

namespace TransitDesk.Core.Services;

/// <summary>Rejestr zgłoszeń w pamięci (Singleton, bezpieczny wątkowo).</summary>
public class InMemoryIncidentService : IIncidentService
{
    private readonly object _sync = new();
    private readonly List<IncidentReport> _reports = [];
    private int _nextId = 1;

    public Task<IncidentReport> RegisterIncidentAsync(IncidentReport report, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            report.Id = _nextId++;
            report.ReportedAtUtc = DateTime.UtcNow;
            _reports.Add(report);
        }

        return Task.FromResult(report);
    }

    public IReadOnlyList<IncidentReport> GetAll()
    {
        lock (_sync)
        {
            return _reports.OrderByDescending(r => r.ReportedAtUtc).ToList();
        }
    }

    public IncidentReport? Find(int id)
    {
        lock (_sync)
        {
            return _reports.FirstOrDefault(r => r.Id == id);
        }
    }
}
