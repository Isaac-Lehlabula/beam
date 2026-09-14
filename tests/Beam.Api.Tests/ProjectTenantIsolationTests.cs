using Beam.Infrastructure;
using Beam.Modules.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Beam.Api.Tests;

/// <summary>
/// Canary tests for the EF Core global query filter that isolates StreamProject by tenant
/// (Beam.Infrastructure/BeamDbContext.cs). This is the single mechanism the whole app relies
/// on to stop one user's data leaking into another's response -- it must never regress silently.
/// </summary>
public class ProjectTenantIsolationTests
{
    private static BeamDbContext CreateContext(string databaseName, Beam.Shared.Abstractions.ICurrentUserContext currentUser)
    {
        var options = new DbContextOptionsBuilder<BeamDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new BeamDbContext(options, currentUser);
    }

    [Fact]
    public async Task User_cannot_see_another_users_personal_project()
    {
        var dbName = Guid.NewGuid().ToString();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        using (var aliceContext = CreateContext(dbName, new FakeCurrentUserContext(alice)))
        {
            aliceContext.Projects.Add(new StreamProject
            {
                Name = "Alice's project",
                OwnerType = OwnerType.User,
                OwnerUserId = alice,
            });
            await aliceContext.SaveChangesAsync();
        }

        using var bobContext = CreateContext(dbName, new FakeCurrentUserContext(bob));
        var visibleToBob = await bobContext.Projects.ToListAsync();

        visibleToBob.Should().BeEmpty();
    }

    [Fact]
    public async Task User_can_see_their_own_personal_project()
    {
        var dbName = Guid.NewGuid().ToString();
        var alice = Guid.NewGuid();

        using (var setupContext = CreateContext(dbName, new FakeCurrentUserContext(alice)))
        {
            setupContext.Projects.Add(new StreamProject
            {
                Name = "Alice's project",
                OwnerType = OwnerType.User,
                OwnerUserId = alice,
            });
            await setupContext.SaveChangesAsync();
        }

        using var aliceContext = CreateContext(dbName, new FakeCurrentUserContext(alice));
        var visible = await aliceContext.Projects.ToListAsync();

        visible.Should().ContainSingle(p => p.Name == "Alice's project");
    }

    [Fact]
    public async Task Organization_member_can_see_orgs_project_but_non_member_cannot()
    {
        var dbName = Guid.NewGuid().ToString();
        var orgId = Guid.NewGuid();
        var member = Guid.NewGuid();
        var outsider = Guid.NewGuid();

        using (var setupContext = CreateContext(dbName, new FakeCurrentUserContext(member, orgId)))
        {
            setupContext.Projects.Add(new StreamProject
            {
                Name = "Org project",
                OwnerType = OwnerType.Organization,
                OwnerOrganizationId = orgId,
            });
            await setupContext.SaveChangesAsync();
        }

        using var memberContext = CreateContext(dbName, new FakeCurrentUserContext(member, orgId));
        (await memberContext.Projects.ToListAsync()).Should().ContainSingle(p => p.Name == "Org project");

        using var outsiderContext = CreateContext(dbName, new FakeCurrentUserContext(outsider));
        (await outsiderContext.Projects.ToListAsync()).Should().BeEmpty();
    }
}
