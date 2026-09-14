using System.Text.Json;
using Beam.Modules.Assets.Abstractions;
using Beam.Modules.Elements.Abstractions;
using Beam.Modules.Elements.Contracts;
using Beam.Modules.Scenes.Abstractions;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Elements.Services;

public class ElementService(
    IElementsDbContext db,
    ISceneAccessService sceneAccess,
    IAssetAccessService assetAccess,
    IRealtimeNotifier realtime) : IElementService
{
    public async Task<List<ElementDto>> ListForSceneAsync(Guid sceneId, CancellationToken ct = default)
    {
        await sceneAccess.EnsureAccessibleAsync(sceneId, ct);
        var elements = await db.Elements.Where(e => e.SceneId == sceneId).OrderBy(e => e.ZIndex).ToListAsync(ct);
        return elements.Select(ElementDto.FromEntity).ToList();
    }

    public async Task<ElementDto> CreateAsync(Guid sceneId, CreateElementRequest request, CancellationToken ct = default)
    {
        await sceneAccess.EnsureAccessibleAsync(sceneId, ct);
        var config = await ParseAndValidateConfigAsync(request.Type, request.Config, ct);
        var nextZIndex = await GetNextZIndexAsync(sceneId, ct);

        var element = new Element
        {
            SceneId = sceneId,
            Type = request.Type,
            Name = request.Name,
            X = request.X,
            Y = request.Y,
            Width = request.Width,
            Height = request.Height,
            ZIndex = nextZIndex,
            Config = ElementConfigSerializer.Serialize(config),
        };

        db.Elements.Add(element);
        await db.SaveChangesAsync(ct);
        await realtime.NotifySceneChangedAsync(sceneId, ct);
        return ElementDto.FromEntity(element);
    }

    public async Task<ElementDto> UpdateTransformAsync(Guid sceneId, Guid elementId, UpdateElementTransformRequest request, CancellationToken ct = default)
    {
        var element = await GetOwnedElementAsync(sceneId, elementId, ct);
        EnsureNotLocked(element, "moved, resized, or otherwise repositioned");
        element.X = request.X;
        element.Y = request.Y;
        element.Width = request.Width;
        element.Height = request.Height;
        element.Rotation = request.Rotation;
        element.ZIndex = request.ZIndex;
        element.Opacity = request.Opacity;
        element.Locked = request.Locked;
        element.Visible = request.Visible;
        element.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await realtime.NotifySceneChangedAsync(sceneId, ct);
        return ElementDto.FromEntity(element);
    }

    public async Task<ElementDto> UpdateConfigAsync(Guid sceneId, Guid elementId, JsonElement config, CancellationToken ct = default)
    {
        var element = await GetOwnedElementAsync(sceneId, elementId, ct);
        var parsed = await ParseAndValidateConfigAsync(element.Type, config, ct);
        var newConfigJson = ElementConfigSerializer.Serialize(parsed);
        EnsureConfigChangesAreWhitelisted(element, newConfigJson);
        element.Config = newConfigJson;
        element.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await realtime.NotifySceneChangedAsync(sceneId, ct);
        return ElementDto.FromEntity(element);
    }

    public async Task<ElementDto> DuplicateAsync(Guid sceneId, Guid elementId, CancellationToken ct = default)
    {
        var source = await GetOwnedElementAsync(sceneId, elementId, ct);
        var copy = new Element
        {
            SceneId = sceneId,
            Type = source.Type,
            Name = $"{source.Name} copy",
            X = source.X + 20,
            Y = source.Y + 20,
            Width = source.Width,
            Height = source.Height,
            Rotation = source.Rotation,
            Opacity = source.Opacity,
            ZIndex = await GetNextZIndexAsync(sceneId, ct),
            Config = source.Config,
        };

        db.Elements.Add(copy);
        await db.SaveChangesAsync(ct);
        await realtime.NotifySceneChangedAsync(sceneId, ct);
        return ElementDto.FromEntity(copy);
    }

    public async Task DeleteAsync(Guid sceneId, Guid elementId, CancellationToken ct = default)
    {
        var element = await GetOwnedElementAsync(sceneId, elementId, ct);
        EnsureNotLocked(element, "deleted");
        db.Elements.Remove(element);
        await db.SaveChangesAsync(ct);
        await realtime.NotifySceneChangedAsync(sceneId, ct);
    }

    private async Task<object> ParseAndValidateConfigAsync(ElementType type, JsonElement raw, CancellationToken ct)
    {
        var config = ElementConfigSerializer.ParseAndValidate(type, raw);

        switch (config)
        {
            case ImageElementConfig image:
                await assetAccess.EnsureAccessibleAsync(image.AssetId, ct);
                break;
            case VideoElementConfig video:
                await assetAccess.EnsureAccessibleAsync(video.AssetId, ct);
                break;
            case ImageCarouselElementConfig carousel:
                foreach (var assetId in carousel.AssetIds)
                {
                    await assetAccess.EnsureAccessibleAsync(assetId, ct);
                }

                break;
            case NowPlayingElementConfig { ArtworkAssetId: { } artworkAssetId }:
                await assetAccess.EnsureAccessibleAsync(artworkAssetId, ct);
                break;
        }

        return config;
    }

    private async Task<int> GetNextZIndexAsync(Guid sceneId, CancellationToken ct)
    {
        var maxZ = await db.Elements.Where(e => e.SceneId == sceneId).Select(e => (int?)e.ZIndex).MaxAsync(ct);
        return (maxZ ?? -1) + 1;
    }

    /// <summary>
    /// Template-lock enforcement (ARCHITECTURE.md §7 "Template locking model"): a non-null
    /// EditableProperties means this element came from an organization template's locked
    /// element. Position/size/rotation/z-order and deletion are ALWAYS forbidden on a locked
    /// element, full stop -- only the config keys named in EditableProperties may change (see
    /// EnsureConfigChangesAreWhitelisted). This matches the brief's example precisely: an
    /// employee can change speaker name/title/camera source (config) but can never move or
    /// remove the company logo/watermark (transform/deletion).
    /// </summary>
    private static void EnsureNotLocked(Element element, string attemptedAction)
    {
        if (element.EditableProperties is not null)
        {
            throw new ForbiddenAccessException($"'{element.Name}' is locked by its template and cannot be {attemptedAction}.");
        }
    }

    private static void EnsureConfigChangesAreWhitelisted(Element element, string newConfigJson)
    {
        if (element.EditableProperties is not { } whitelist)
        {
            return;
        }

        using var oldDoc = JsonDocument.Parse(element.Config);
        using var newDoc = JsonDocument.Parse(newConfigJson);

        foreach (var newProperty in newDoc.RootElement.EnumerateObject())
        {
            var oldHasSameValue = oldDoc.RootElement.TryGetProperty(newProperty.Name, out var oldValue)
                && JsonElementDeepEquals(oldValue, newProperty.Value);

            if (!oldHasSameValue && !whitelist.Contains(newProperty.Name))
            {
                throw new ForbiddenAccessException(
                    $"'{element.Name}' is locked by its template: '{newProperty.Name}' cannot be changed.");
            }
        }
    }

    private static bool JsonElementDeepEquals(JsonElement a, JsonElement b) => a.GetRawText() == b.GetRawText();

    private async Task<Element> GetOwnedElementAsync(Guid sceneId, Guid elementId, CancellationToken ct)
    {
        await sceneAccess.EnsureAccessibleAsync(sceneId, ct);
        var element = await db.Elements.FirstOrDefaultAsync(e => e.Id == elementId && e.SceneId == sceneId, ct);
        if (element is null)
        {
            throw new NotFoundException(nameof(Element), elementId);
        }

        return element;
    }
}
