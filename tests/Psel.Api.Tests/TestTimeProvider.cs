namespace Psel.Api.Tests;

/// <summary>
/// Deterministic clock for examination expiry tests. Tests advance the clock
/// explicitly so time-limit behaviour never depends on the wall clock.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset utcNow = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void SetUtcNow(DateTimeOffset value) => utcNow = value;

    public void Advance(TimeSpan delta) => utcNow += delta;
}
