using Beam.Modules.Assets.Abstractions;
using Beam.Modules.Assets.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController(IAssetService assetService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<AssetDto>>> List(CancellationToken ct) =>
        Ok(await assetService.ListAccessibleAsync(ct));

    private static readonly HashSet<string> VideoContentTypes = ["video/mp4", "video/webm"];

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<AssetDto>> Upload(IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        var asset = VideoContentTypes.Contains(file.ContentType)
            ? await assetService.UploadVideoAsync(stream, file.FileName, file.ContentType, ct)
            : await assetService.UploadImageAsync(stream, file.FileName, file.ContentType, ct);
        return CreatedAtAction(nameof(List), asset);
    }

    // Anonymous: asset files back public overlay elements and are served the same way the
    // overlay itself is (see AssetService.GetFileAsync) -- a capability by unguessable GUID.
    [AllowAnonymous]
    [EnableCors("PublicOverlay")]
    [HttpGet("{assetId:guid}/file")]
    public async Task<IActionResult> GetFile(Guid assetId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await assetService.GetFileAsync(assetId, ct);
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(content, contentType, fileName);
    }

    [Authorize]
    [HttpDelete("{assetId:guid}")]
    public async Task<IActionResult> Delete(Guid assetId, CancellationToken ct)
    {
        await assetService.DeleteAsync(assetId, ct);
        return NoContent();
    }
}
