using Amazon.S3;
using Amazon.S3.Model;
using Beam.Modules.Assets.Abstractions;
using Microsoft.Extensions.Options;

namespace Beam.Infrastructure.Storage;

public class S3StorageOptions
{
    public string BucketName { get; set; } = "beam-assets";
}

public class S3FileStorageService(IAmazonS3 s3, IOptions<S3StorageOptions> options) : IFileStorageService
{
    private string BucketName => options.Value.BucketName;

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        }, ct);
    }

    public async Task<(Stream Content, string ContentType)?> ReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await s3.GetObjectAsync(BucketName, key, ct);
            var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            return (buffer, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await s3.DeleteObjectAsync(BucketName, key, ct);
    }
}
