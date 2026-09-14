namespace Beam.Shared.Entities;

public abstract class EntityBase
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
