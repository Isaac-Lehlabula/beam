using Beam.Infrastructure;
using Beam.Modules.LiveControl.Services;
using Beam.Modules.Organizations;
using Beam.Modules.Organizations.Contracts;
using Beam.Modules.Organizations.Services;
using Beam.Modules.Projects;
using Beam.Modules.Projects.Services;
using Beam.Modules.Scenes;
using Beam.Modules.Scenes.Services;
using Beam.Shared.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Beam.Api.Tests;

/// <summary>
/// Phase 12: live-control and org-admin actions must leave a trail in AuditLogEntry (see
/// Beam.Modules.Audit). These exercise the real services against an InMemory BeamDbContext,
/// same pattern as ProjectTenantIsolationTests.
/// </summary>
public class AuditLoggingTests
{
    private static BeamDbContext CreateContext(string dbName, ICurrentUserContext currentUser) =>
        new(new DbContextOptionsBuilder<BeamDbContext>().UseInMemoryDatabase(dbName).Options, currentUser);

    private class FakeEmailLookup(Guid resolvesTo) : IUserLookupService
    {
        public Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult<Guid?>(resolvesTo);

        public Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<string?>("Test User");
    }

    [Fact]
    public async Task Starting_a_live_session_writes_an_audit_entry()
    {
        var dbName = Guid.NewGuid().ToString();
        var owner = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var db = CreateContext(dbName, currentUser);

        var projectService = new ProjectService(db, currentUser, new OrganizationService(db, currentUser, new FakeUserLookupService(), MakeAudit(db, currentUser), MakeAudit(db, currentUser)));
        var project = await projectService.CreateAsync(new Beam.Modules.Projects.Contracts.CreateProjectRequest("A show", 1920, 1080, null));

        var sceneService = new SceneService(db, projectService);
        var auditLogger = MakeAudit(db, currentUser);
        var liveControl = new LiveControlService(db, new InMemoryLiveStateStore(), projectService, sceneService, new FakeRealtimeNotifier(), auditLogger);

        await liveControl.StartSessionAsync(project.Id);

        db.AuditLogEntries.Should().ContainSingle(a => a.Action == "live_session.start" && a.TargetId == project.Id && a.ActorUserId == owner);
    }

    [Fact]
    public async Task Switching_scenes_writes_an_audit_entry_with_the_scene_id_in_metadata()
    {
        var dbName = Guid.NewGuid().ToString();
        var owner = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var db = CreateContext(dbName, currentUser);

        var orgAccess = new OrganizationService(db, currentUser, new FakeUserLookupService(), MakeAudit(db, currentUser), MakeAudit(db, currentUser));
        var projectService = new ProjectService(db, currentUser, orgAccess);
        var project = await projectService.CreateAsync(new Beam.Modules.Projects.Contracts.CreateProjectRequest("A show", 1920, 1080, null));

        var sceneService = new SceneService(db, projectService);
        var scene = await sceneService.CreateAsync(project.Id, new Beam.Modules.Scenes.Contracts.CreateSceneRequest("Main"));

        var auditLogger = MakeAudit(db, currentUser);
        var liveControl = new LiveControlService(db, new InMemoryLiveStateStore(), projectService, sceneService, new FakeRealtimeNotifier(), auditLogger);

        await liveControl.SwitchSceneAsync(project.Id, scene.Id);

        var entry = db.AuditLogEntries.Should().ContainSingle(a => a.Action == "live_session.switch_scene").Subject;
        entry.MetadataJson.Should().Contain(scene.Id.ToString());
    }

    [Fact]
    public async Task Adding_an_organization_member_writes_an_audit_entry_tagged_with_the_organization()
    {
        var dbName = Guid.NewGuid().ToString();
        var owner = Guid.NewGuid();
        var invitee = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var db = CreateContext(dbName, currentUser);

        var auditLogger = MakeAudit(db, currentUser);
        var orgService = new OrganizationService(db, currentUser, new FakeEmailLookup(invitee), auditLogger, auditLogger);
        var org = await orgService.CreateAsync(new CreateOrganizationRequest("Acme"));

        await orgService.AddMemberAsync(org.Id, new AddMemberRequest("invitee@example.com", OrganizationRole.Producer));

        db.AuditLogEntries.Should().ContainSingle(a =>
            a.Action == "organization.member.add" && a.OrganizationId == org.Id && a.ActorUserId == owner);
    }

    [Fact]
    public async Task Owner_can_list_their_organizations_audit_log_but_a_non_admin_cannot()
    {
        var dbName = Guid.NewGuid().ToString();
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var ownerContext = new FakeCurrentUserContext(owner);
        var db = CreateContext(dbName, ownerContext);

        var auditLogger = MakeAudit(db, ownerContext);
        var orgService = new OrganizationService(db, ownerContext, new FakeEmailLookup(member), auditLogger, auditLogger);
        var org = await orgService.CreateAsync(new CreateOrganizationRequest("Acme"));
        await orgService.AddMemberAsync(org.Id, new AddMemberRequest("member@example.com", OrganizationRole.Streamer));

        (await orgService.ListAuditLogAsync(org.Id, 50)).Should().NotBeEmpty();

        var memberContext = new FakeCurrentUserContext(member);
        var memberDb = CreateContext(dbName, memberContext);
        var memberAudit = MakeAudit(memberDb, memberContext);
        var memberOrgService = new OrganizationService(memberDb, memberContext, new FakeEmailLookup(member), memberAudit, memberAudit);

        var act = () => memberOrgService.ListAuditLogAsync(org.Id, 50);
        await act.Should().ThrowAsync<Beam.Shared.Exceptions.ForbiddenAccessException>();
    }

    private static Beam.Modules.Audit.Services.AuditLogger MakeAudit(BeamDbContext db, ICurrentUserContext currentUser) => new(db, currentUser);
}
