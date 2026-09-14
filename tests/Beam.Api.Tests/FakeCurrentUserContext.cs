using Beam.Shared.Abstractions;

namespace Beam.Api.Tests;

public class FakeCurrentUserContext(Guid? userId, params Guid[] organizationIds) : ICurrentUserContext
{
    public bool IsAuthenticated => userId is not null;

    public Guid? UserId => userId;

    public IReadOnlyCollection<Guid> OrganizationIds { get; } = organizationIds;
}
