namespace Beam.Modules.Assets.Abstractions;

/// <summary>Public contract other modules (Elements, ...) use to validate an asset reference.</summary>
public interface IAssetAccessService
{
    /// <summary>Throws NotFoundException if the asset doesn't exist or isn't owned by the current tenant.</summary>
    Task<Asset> EnsureAccessibleAsync(Guid assetId, CancellationToken ct = default);
}
