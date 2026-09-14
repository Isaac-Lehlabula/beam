using Beam.Modules.Identity;
using Beam.Shared.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Beam.Api.Auth;

public class UserLookupService(UserManager<BeamUser> userManager) : IUserLookupService
{
    public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.DisplayName;
    }
}
