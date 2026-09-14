namespace Beam.Shared.Abstractions;

/// <summary>Anonymous/unauthenticated context. Used for design-time EF tooling and as a safe default.</summary>
public sealed class NullCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public IReadOnlyCollection<Guid> OrganizationIds => Array.Empty<Guid>();
}
