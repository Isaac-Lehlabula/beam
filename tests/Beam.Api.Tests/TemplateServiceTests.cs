using Beam.Infrastructure;
using Beam.Modules.Organizations.Services;
using Beam.Modules.Projects.Services;
using Beam.Modules.Templates;
using Beam.Modules.Templates.Contracts;
using Beam.Modules.Templates.Seed;
using Beam.Modules.Templates.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Beam.Api.Tests;

public class TemplateServiceTests
{
    private static (BeamDbContext Db, TemplateService Service) CreateFixture(Guid userId)
    {
        var currentUser = new FakeCurrentUserContext(userId);
        var options = new DbContextOptionsBuilder<BeamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BeamDbContext(options, currentUser);
        db.Templates.AddRange(TemplateSeedData.GetSeedTemplates());
        db.SaveChanges();

        var auditLogger = new Beam.Modules.Audit.Services.AuditLogger(db, currentUser);
        var organizationAccess = new OrganizationService(db, currentUser, new FakeUserLookupService(), auditLogger, auditLogger);
        var projectAccess = new ProjectService(db, currentUser, organizationAccess);
        return (db, new TemplateService(db, db, db, db, projectAccess, organizationAccess, currentUser));
    }

    [Fact]
    public async Task Instantiating_the_gaming_template_creates_four_scenes_matching_its_definition()
    {
        var (db, service) = CreateFixture(Guid.NewGuid());

        var project = await service.InstantiateAsync(
            TemplateSeedData.GamingTemplateId,
            new InstantiateTemplateRequest("My Stream", 1920, 1080));

        var scenes = await db.Scenes.Where(s => s.ProjectId == project.Id).OrderBy(s => s.OrderIndex).ToListAsync();
        scenes.Select(s => s.Name).Should().Equal("Starting Soon", "Main", "Break", "Ending");

        var startingSoonElements = await db.Elements.Where(e => e.SceneId == scenes[0].Id).ToListAsync();
        startingSoonElements.Should().HaveCount(2);
        startingSoonElements.Should().OnlyContain(e => e.Type == Beam.Modules.Elements.ElementType.Text);
    }

    [Fact]
    public async Task System_template_elements_are_not_locked()
    {
        var (db, service) = CreateFixture(Guid.NewGuid());

        var project = await service.InstantiateAsync(
            TemplateSeedData.PodcastTemplateId,
            new InstantiateTemplateRequest("My Podcast", 1920, 1080));

        var elements = await db.Elements.Where(e => e.Scene!.ProjectId == project.Id).ToListAsync();
        elements.Should().OnlyContain(e => e.EditableProperties == null);
    }

    [Fact]
    public async Task Unknown_template_id_throws_not_found()
    {
        var (_, service) = CreateFixture(Guid.NewGuid());

        var act = () => service.InstantiateAsync(Guid.NewGuid(), new InstantiateTemplateRequest("X", 1920, 1080));

        await act.Should().ThrowAsync<Beam.Shared.Exceptions.NotFoundException>();
    }
}
