using Microsoft.AspNetCore.Identity;

namespace Beam.Modules.Identity;

public class BeamUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
