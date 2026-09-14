using Beam.Infrastructure;
using Beam.Modules.Projects;
using Beam.Modules.Scenes.Contracts;
using Beam.Modules.Scenes.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Beam.Api.Tests;

public class SceneServiceTests
{
    private static (BeamDbContext Db, SceneService Service, StreamProject Project) CreateFixture()
    {
        var owner = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var options = new DbContextOptionsBuilder<BeamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BeamDbContext(options, currentUser);

        var project = new StreamProject { Name = "Test project", OwnerType = OwnerType.User, OwnerUserId = owner };
        db.Projects.Add(project);
        db.SaveChanges();

        var projectAccess = new TestProjectAccessService(db);
        return (db, new SceneService(db, projectAccess), project);
    }

    [Fact]
    public async Task Newly_created_scenes_get_sequential_order_indexes()
    {
        var (_, service, project) = CreateFixture();

        var first = await service.CreateAsync(project.Id, new CreateSceneRequest("Starting Soon"));
        var second = await service.CreateAsync(project.Id, new CreateSceneRequest("Main"));
        var third = await service.CreateAsync(project.Id, new CreateSceneRequest("Ending"));

        first.OrderIndex.Should().Be(0);
        second.OrderIndex.Should().Be(1);
        third.OrderIndex.Should().Be(2);
    }

    [Fact]
    public async Task Reorder_applies_the_requested_order()
    {
        var (_, service, project) = CreateFixture();
        var a = await service.CreateAsync(project.Id, new CreateSceneRequest("A"));
        var b = await service.CreateAsync(project.Id, new CreateSceneRequest("B"));
        var c = await service.CreateAsync(project.Id, new CreateSceneRequest("C"));

        await service.ReorderAsync(project.Id, new ReorderScenesRequest([c.Id, a.Id, b.Id]));

        var ordered = await service.ListForProjectAsync(project.Id);
        ordered.Select(s => s.Id).Should().Equal(c.Id, a.Id, b.Id);
    }

    [Fact]
    public async Task Duplicate_appends_a_copy_at_the_end_with_a_new_id()
    {
        var (_, service, project) = CreateFixture();
        var original = await service.CreateAsync(project.Id, new CreateSceneRequest("Main"));

        var copy = await service.DuplicateAsync(project.Id, original.Id);

        copy.Id.Should().NotBe(original.Id);
        copy.Name.Should().Be("Main copy");
        copy.OrderIndex.Should().Be(1);
    }

    /// <summary>Minimal stand-in for the real cross-module ProjectService, scoped to what SceneService needs.</summary>
    private class TestProjectAccessService(BeamDbContext db) : Beam.Modules.Projects.Abstractions.IProjectAccessService
    {
        public async Task<StreamProject> EnsureAccessibleAsync(Guid projectId, CancellationToken ct = default) =>
            await db.Projects.FirstAsync(p => p.Id == projectId, ct);
    }
}
