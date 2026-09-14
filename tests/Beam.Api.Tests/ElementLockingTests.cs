using Beam.Infrastructure;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Contracts;
using Beam.Modules.Elements.Services;
using Beam.Modules.Projects;
using Beam.Modules.Scenes;
using Beam.Modules.Scenes.Services;
using Beam.Shared.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Beam.Api.Tests;

/// <summary>
/// The brief's exact scenario: an org template locks a company logo/watermark so an employee
/// can never move, resize, or delete it, while still being free to edit whitelisted config
/// (e.g. speaker name) on a locked element. See ElementService.EnsureNotLocked /
/// EnsureConfigChangesAreWhitelisted.
/// </summary>
public class ElementLockingTests
{
    private static (ElementService Service, Guid SceneId, Guid LockedLogoId, Guid LockedSpeakerNameId) CreateFixture()
    {
        var owner = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var options = new DbContextOptionsBuilder<BeamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BeamDbContext(options, currentUser);

        var project = new StreamProject { Name = "Corporate Livestream", OwnerType = OwnerType.User, OwnerUserId = owner };
        db.Projects.Add(project);
        var scene = new Scene { ProjectId = project.Id, Name = "Main", OrderIndex = 0 };
        db.Scenes.Add(scene);

        var logo = new Element
        {
            SceneId = scene.Id,
            Type = ElementType.Image,
            Name = "Company Logo",
            X = 20,
            Y = 20,
            Width = 150,
            Height = 150,
            Config = "{\"assetId\":\"" + Guid.NewGuid() + "\",\"fit\":\"contain\"}",
            SourceTemplateElementId = Guid.NewGuid(),
            EditableProperties = [], // locked: no config keys editable, transform/delete always forbidden
        };
        var speakerName = new Element
        {
            SceneId = scene.Id,
            Type = ElementType.Text,
            Name = "Speaker Name",
            X = 100,
            Y = 900,
            Width = 400,
            Height = 60,
            Config = "{\"content\":\"Jane Doe\",\"fontFamily\":\"Inter\",\"fontSize\":32,\"fontWeight\":\"700\",\"textAlign\":\"left\",\"color\":\"#fff\",\"backgroundColor\":null}",
            SourceTemplateElementId = Guid.NewGuid(),
            EditableProperties = ["content"], // locked, but "content" (the speaker's name) may change
        };
        db.Elements.AddRange(logo, speakerName);
        db.SaveChanges();

        var auditLogger = new Beam.Modules.Audit.Services.AuditLogger(db, currentUser);
        var sceneService = new SceneService(db, new Beam.Modules.Projects.Services.ProjectService(
            db, currentUser, new Beam.Modules.Organizations.Services.OrganizationService(db, currentUser, new FakeUserLookupService(), auditLogger, auditLogger)));
        var elementService = new ElementService(db, sceneService, new FakeAssetAccessService(), new FakeRealtimeNotifier());

        return (elementService, scene.Id, logo.Id, speakerName.Id);
    }

    [Fact]
    public async Task Locked_element_cannot_be_moved()
    {
        var (service, sceneId, logoId, _) = CreateFixture();

        var act = () => service.UpdateTransformAsync(sceneId, logoId,
            new UpdateElementTransformRequest(500, 500, 150, 150, 0, 0, 1, false, true));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Locked_element_cannot_be_deleted()
    {
        var (service, sceneId, logoId, _) = CreateFixture();

        var act = () => service.DeleteAsync(sceneId, logoId);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Locked_element_config_cannot_change_a_non_whitelisted_property()
    {
        var (service, sceneId, logoId, _) = CreateFixture();
        var newConfig = System.Text.Json.JsonSerializer.SerializeToElement(
            new ImageElementConfig(Guid.NewGuid(), "cover")); // different fit -- not whitelisted

        var act = () => service.UpdateConfigAsync(sceneId, logoId, newConfig);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Locked_element_config_CAN_change_a_whitelisted_property()
    {
        var (service, sceneId, _, speakerNameId) = CreateFixture();
        var newConfig = System.Text.Json.JsonSerializer.SerializeToElement(
            new TextElementConfig("John Smith", "Inter", 32, "700", "left", "#fff", null));

        var result = await service.UpdateConfigAsync(sceneId, speakerNameId, newConfig);

        ((TextElementConfig)result.Config).Content.Should().Be("John Smith");
    }

    [Fact]
    public async Task Unlocked_elements_are_unaffected()
    {
        var owner = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var options = new DbContextOptionsBuilder<BeamDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new BeamDbContext(options, currentUser);
        var project = new StreamProject { Name = "P", OwnerType = OwnerType.User, OwnerUserId = owner };
        db.Projects.Add(project);
        var scene = new Scene { ProjectId = project.Id, Name = "Main", OrderIndex = 0 };
        db.Scenes.Add(scene);
        var element = new Element { SceneId = scene.Id, Type = ElementType.Text, Name = "Free", Config = "{\"content\":\"x\",\"fontFamily\":\"Inter\",\"fontSize\":16,\"fontWeight\":\"400\",\"textAlign\":\"left\",\"color\":\"#fff\",\"backgroundColor\":null}" };
        db.Elements.Add(element);
        db.SaveChanges();

        var auditLogger = new Beam.Modules.Audit.Services.AuditLogger(db, currentUser);
        var sceneService = new SceneService(db, new Beam.Modules.Projects.Services.ProjectService(
            db, currentUser, new Beam.Modules.Organizations.Services.OrganizationService(db, currentUser, new FakeUserLookupService(), auditLogger, auditLogger)));
        var service = new ElementService(db, sceneService, new FakeAssetAccessService(), new FakeRealtimeNotifier());

        var result = await service.UpdateTransformAsync(scene.Id, element.Id,
            new UpdateElementTransformRequest(999, 999, 200, 200, 45, 3, 0.5, false, true));

        result.X.Should().Be(999);
    }
}
