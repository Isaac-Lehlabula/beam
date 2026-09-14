using Beam.Shared.Entities;

namespace Beam.Modules.Organizations;

/// <summary>One per organization: the shared colors/fonts/logo/watermark org templates draw on.</summary>
public class BrandKit : EntityBase
{
    public Guid OrganizationId { get; set; }

    public List<string> Colors { get; set; } = [];

    public List<string> Fonts { get; set; } = [];

    public Guid? LogoAssetId { get; set; }

    public Guid? WatermarkAssetId { get; set; }
}
