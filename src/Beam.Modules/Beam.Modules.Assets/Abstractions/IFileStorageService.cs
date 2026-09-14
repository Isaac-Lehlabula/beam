namespace Beam.Modules.Assets.Abstractions;

/// <summary>
/// Thin abstraction over object storage (MinIO locally, S3/R2 in prod) so this module and its
/// consumers never take a compile-time dependency on a specific storage SDK.
/// </summary>
public interface IFileStorageService
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    Task<(Stream Content, string ContentType)?> ReadAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
