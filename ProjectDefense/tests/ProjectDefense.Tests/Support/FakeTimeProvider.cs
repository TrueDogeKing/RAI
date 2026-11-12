namespace ProjectDefense.Tests.Support;

public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTime localNow)
    {
        _now = new DateTimeOffset(DateTime.SpecifyKind(localNow, DateTimeKind.Unspecified), TimeSpan.Zero);
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;
}
