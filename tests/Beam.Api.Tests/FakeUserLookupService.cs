using Beam.Shared.Abstractions;

namespace Beam.Api.Tests;

public class FakeUserLookupService : IUserLookupService
{
    public Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult<Guid?>(null);

    public Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<string?>("Test User");
}
