namespace Beam.Modules.Assets.Contracts;

public record AssetDto(Guid Id, string FileName, string ContentType, long SizeBytes, string Url, DateTimeOffset CreatedAtUtc)
{
    public static AssetDto FromEntity(Asset asset) => new(
        asset.Id,
        asset.FileName,
        asset.ContentType,
        asset.SizeBytes,
        $"/api/assets/{asset.Id}/file",
        asset.CreatedAtUtc);
}
