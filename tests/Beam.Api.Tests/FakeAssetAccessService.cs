using Beam.Modules.Assets;
using Beam.Modules.Assets.Abstractions;

namespace Beam.Api.Tests;

/// <summary>Treats every asset id as existing/accessible -- these tests aren't exercising asset validation.</summary>
public class FakeAssetAccessService : IAssetAccessService
{
    public Task<Asset> EnsureAccessibleAsync(Guid assetId, CancellationToken ct = default) =>
        Task.FromResult(new Asset { Id = assetId, Type = AssetType.Image });
}
