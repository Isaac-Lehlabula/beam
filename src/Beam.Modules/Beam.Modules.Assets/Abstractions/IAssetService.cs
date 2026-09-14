using Beam.Modules.Assets.Contracts;

namespace Beam.Modules.Assets.Abstractions;

public interface IAssetService
{
    Task<List<AssetDto>> ListAccessibleAsync(CancellationToken ct = default);

    Task<AssetDto> UploadImageAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default);

    /// <summary>
    /// Unlike images, video is not decoded/re-encoded server-side (no equivalent of ImageSharp
    /// in this stack for video) -- validation is limited to content-type, a magic-byte sniff of
    /// the container format, and a size cap. This is a smaller trust boundary than image upload;
    /// see AssetService.UploadVideoAsync's doc comment.
    /// </summary>
    Task<AssetDto> UploadVideoAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default);

    Task<(Stream Content, string ContentType, string FileName)> GetFileAsync(Guid assetId, CancellationToken ct = default);

    Task DeleteAsync(Guid assetId, CancellationToken ct = default);
}
