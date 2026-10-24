using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TransitDesk.Web.Infrastructure;
using TransitDesk.Web.Models;

namespace TransitDesk.Tests;

public class SessionExtensionsTests
{
    [Fact]
    public void SetObject_ThenGetObject_RoundTripsComplexWatchlist()
    {
        var session = new FakeSession();
        var watchlist = new List<WatchedVehicle>
        {
            new() { SideNumber = 1001, ModelName = "Pesa Swing 120NaG", DepotName = "Wrzeszcz", AddedAtUtc = new DateTime(2026, 10, 23, 8, 15, 30, DateTimeKind.Utc) },
            new() { SideNumber = 2072, ModelName = "Solaris Urbino 12", DepotName = "Hallera", AddedAtUtc = new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc) }
        };

        session.SetObject(SessionKeys.Watchlist, watchlist);
        var restored = session.GetObject<List<WatchedVehicle>>(SessionKeys.Watchlist);

        restored.Should().NotBeSameAs(watchlist);
        restored.Should().BeEquivalentTo(watchlist, options => options.WithStrictOrdering());
    }

    [Fact]
    public void GetObject_MissingKey_ReturnsDefault()
    {
        var session = new FakeSession();

        session.GetObject<List<WatchedVehicle>>("brak-klucza").Should().BeNull();
    }

    [Fact]
    public void SetObject_PreservesPolishCharacters()
    {
        var session = new FakeSession();
        var vehicle = new WatchedVehicle { SideNumber = 1047, ModelName = "Żółty tramwaj – ąęśćńół", DepotName = "Nowy Port" };

        session.SetObject("v", vehicle);

        session.GetObject<WatchedVehicle>("v")!.ModelName.Should().Be("Żółty tramwaj – ąęśćńół");
    }

    /// <summary>Minimalna implementacja ISession w pamięci – SetString/GetString to rozszerzenia nad Set/TryGetValue.</summary>
    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }
}
