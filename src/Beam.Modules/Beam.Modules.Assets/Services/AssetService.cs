using Beam.Modules.Assets.Abstractions;
using Beam.Modules.Assets.Contracts;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace Beam.Modules.Assets.Services;

public class AssetService(IAssetsDbContext db, IFileStorageService storage, ICurrentUserContext currentUser)
    : IAssetService, IAssetAccessService
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private const long MaxVideoUploadBytes = 100 * 1024 * 1024;

    public async Task<List<AssetDto>> ListAccessibleAsync(CancellationToken ct = default) =>
        await db.Assets
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => AssetDto.FromEntity(a))
            .ToListAsync(ct);

    public async Task<Asset> EnsureAccessibleAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null)
        {
            throw new NotFoundException(nameof(Asset), assetId);
        }

        return asset;
    }

    public async Task<AssetDto> UploadImageAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to upload assets.");
        }

        if (content.Length > MaxUploadBytes)
        {
            throw new ValidationAppException($"Image must be {MaxUploadBytes / (1024 * 1024)}MB or smaller.");
        }

        // Decoding (rather than trusting the declared content-type) is the actual security
        // boundary here: it rejects anything that isn't a genuine, decodable raster image --
        // including an SVG or HTML file wearing an image/* content-type to smuggle script.
        Image image;
        try
        {
            image = await Image.LoadAsync(content, ct);
        }
        catch (UnknownImageFormatException)
        {
            throw new ValidationAppException("The uploaded file is not a valid image.");
        }
        catch (InvalidImageContentException)
        {
            throw new ValidationAppException("The uploaded file is not a valid image.");
        }

        using var _ = image;
        var (encoder, contentType, extension) = ChooseEncoder(image.Metadata.DecodedImageFormat?.Name);

        using var reencoded = new MemoryStream();
        await image.SaveAsync(reencoded, encoder, ct);
        reencoded.Position = 0;

        var storageKey = $"assets/{Guid.NewGuid()}{extension}";
        await storage.SaveAsync(storageKey, reencoded, contentType, ct);

        var asset = new Asset
        {
            OwnerType = AssetOwnerType.User,
            OwnerUserId = currentUser.UserId,
            Type = AssetType.Image,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = reencoded.Length,
            StorageKey = storageKey,
        };

        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
        return AssetDto.FromEntity(asset);
    }

    /// <summary>
    /// No ImageSharp-equivalent decode/re-encode step exists for video in this stack, so unlike
    /// UploadImageAsync this cannot fully neutralize a malicious payload by construction. The
    /// mitigations here are: a magic-byte sniff of the container format (rejects anything that
    /// isn't a genuine MP4/WebM box header, closing the "renamed .html as .mp4" class of attack),
    /// a content-type allowlist, and a larger-but-still-bounded size cap. This is a real,
    /// disclosed gap versus image upload -- full validation would need a video-processing
    /// dependency (e.g. shelling out to ffprobe) that isn't part of this stack yet.
    /// </summary>
    public async Task<AssetDto> UploadVideoAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to upload assets.");
        }

        if (content.Length > MaxVideoUploadBytes)
        {
            throw new ValidationAppException($"Video must be {MaxVideoUploadBytes / (1024 * 1024)}MB or smaller.");
        }

        var extension = declaredContentType switch
        {
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            _ => throw new ValidationAppException("Unsupported video format. Use MP4 or WebM."),
        };

        var header = new byte[12];
        var read = await content.ReadAsync(header.AsMemory(0, 12), ct);
        content.Position = 0;
        if (read < 12 || !LooksLikeContainer(header, declaredContentType))
        {
            throw new ValidationAppException("The uploaded file is not a valid video.");
        }

        var storageKey = $"assets/{Guid.NewGuid()}{extension}";
        await storage.SaveAsync(storageKey, content, declaredContentType, ct);

        var asset = new Asset
        {
            OwnerType = AssetOwnerType.User,
            OwnerUserId = currentUser.UserId,
            Type = AssetType.Video,
            FileName = fileName,
            ContentType = declaredContentType,
            SizeBytes = content.Length,
            StorageKey = storageKey,
        };

        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
        return AssetDto.FromEntity(asset);
    }

    private static bool LooksLikeContainer(byte[] header, string contentType) => contentType switch
    {
        // MP4/ISOBMFF: bytes 4-7 are "ftyp".
        "video/mp4" => header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p',
        // WebM/Matroska: EBML magic number.
        "video/webm" => header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
        _ => false,
    };

    public async Task<(Stream Content, string ContentType, string FileName)> GetFileAsync(Guid assetId, CancellationToken ct = default)
    {
        // Deliberately not tenant-filtered: an asset backs a public overlay element, so its
        // file bytes are served like the overlay itself -- a capability by unguessable GUID,
        // not a login-gated resource. See ARCHITECTURE.md "Overlay Rendering Architecture".
        var asset = await db.Assets.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null)
        {
            throw new NotFoundException(nameof(Asset), assetId);
        }

        var file = await storage.ReadAsync(asset.StorageKey, ct);
        if (file is null)
        {
            throw new NotFoundException(nameof(Asset), assetId);
        }

        return (file.Value.Content, file.Value.ContentType, asset.FileName);
    }

    public async Task DeleteAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await EnsureAccessibleAsync(assetId, ct);
        await storage.DeleteAsync(asset.StorageKey, ct);
        db.Assets.Remove(asset);
        await db.SaveChangesAsync(ct);
    }

    private static (SixLabors.ImageSharp.Formats.IImageEncoder Encoder, string ContentType, string Extension) ChooseEncoder(string? formatName) =>
        formatName switch
        {
            "PNG" => (new PngEncoder(), "image/png", ".png"),
            "JPEG" => (new JpegEncoder(), "image/jpeg", ".jpg"),
            "WEBP" => (new WebpEncoder(), "image/webp", ".webp"),
            _ => throw new ValidationAppException("Unsupported image format. Use PNG, JPEG, or WebP."),
        };
}
