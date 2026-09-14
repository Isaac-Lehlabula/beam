using Beam.Shared.Entities;

namespace Beam.Modules.Assets;

public enum AssetOwnerType
{
    User = 0,
    Organization = 1,
}

public enum AssetType
{
    Image = 0,
    Video = 1,
}

public class Asset : EntityBase
{
    public AssetOwnerType OwnerType { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid? OwnerOrganizationId { get; set; }

    public AssetType Type { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>Object storage key; never exposed to clients directly (they use the /api/assets/{id}/file endpoint).</summary>
    public string StorageKey { get; set; } = string.Empty;
}
