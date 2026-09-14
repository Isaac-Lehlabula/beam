namespace Beam.Shared.Abstractions;

/// <summary>
/// Lets modules resolve a user by email/id without depending on ASP.NET Core Identity's
/// UserManager directly -- that's an Api-layer concern (Beam.Api/Auth/UserLookupService).
/// </summary>
public interface IUserLookupService
{
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default);

    Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default);
}
