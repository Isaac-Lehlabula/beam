using Beam.Modules.Assets;
using Beam.Modules.Assets.Abstractions;
using Beam.Modules.Audit;
using Beam.Modules.Audit.Abstractions;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Abstractions;
using Beam.Modules.Identity;
using Beam.Modules.LiveControl;
using Beam.Modules.LiveControl.Abstractions;
using Beam.Modules.Organizations;
using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Overlays;
using Beam.Modules.Overlays.Abstractions;
using Beam.Modules.Projects;
using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Scenes;
using Beam.Modules.Scenes.Abstractions;
using Beam.Modules.StreamingDestinations;
using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Modules.Templates;
using Beam.Modules.Templates.Abstractions;
using Beam.Modules.Templates.Seed;
using Beam.Shared.Abstractions;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Beam.Infrastructure;

/// <summary>
/// The single EF Core DbContext for the whole application. It aggregates every module's
/// entities (unavoidable in a modular monolith sharing one database/migration history) but
/// modules never reference this type directly — they depend on the narrow
/// I*DbContext interfaces this class implements, so the module→Infrastructure dependency
/// direction never inverts.
/// </summary>
public class BeamDbContext(DbContextOptions<BeamDbContext> options, ICurrentUserContext currentUser)
    : IdentityDbContext<BeamUser, IdentityRole<Guid>, Guid>(options),
        IOrganizationsDbContext,
        IProjectsDbContext,
        IScenesDbContext,
        IElementsDbContext,
        IAssetsDbContext,
        IOverlaysDbContext,
        ITemplatesDbContext,
        ILiveControlDbContext,
        IStreamingDestinationsDbContext,
        IAuditDbContext,
        IDataProtectionKeyContext
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<BrandKit> BrandKits => Set<BrandKit>();

    public DbSet<StreamProject> Projects => Set<StreamProject>();

    public DbSet<Scene> Scenes => Set<Scene>();

    public DbSet<Element> Elements => Set<Element>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<OverlayToken> OverlayTokens => Set<OverlayToken>();

    public DbSet<Template> Templates => Set<Template>();

    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();

    public DbSet<StreamingDestinationConnection> StreamingDestinationConnections => Set<StreamingDestinationConnection>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    // ASP.NET Core Data Protection key ring (auth cookie + stored OAuth token encryption). Kept
    // in the database rather than on disk so the API needs no persistent volume -- see Program.cs.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BeamUser>().Property(u => u.DisplayName).HasMaxLength(120);

        modelBuilder.Entity<Organization>(b =>
        {
            b.Property(o => o.Name).HasMaxLength(120).IsRequired();

            // Visible only to its own members -- expressed as a subquery over
            // OrganizationMemberships since Organization has no owner column of its own.
            b.HasQueryFilter(o => Set<OrganizationMembership>().Any(m => m.OrganizationId == o.Id && m.UserId == currentUser.UserId));
        });

        modelBuilder.Entity<OrganizationMembership>(b =>
        {
            b.HasOne(m => m.Organization).WithMany().HasForeignKey(m => m.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();
        });

        modelBuilder.Entity<BrandKit>(b =>
        {
            b.HasIndex(k => k.OrganizationId).IsUnique();
            // Same membership-subquery visibility rule as Organization itself.
            b.HasQueryFilter(k => Set<OrganizationMembership>().Any(m => m.OrganizationId == k.OrganizationId && m.UserId == currentUser.UserId));
        });

        modelBuilder.Entity<StreamProject>(b =>
        {
            b.Property(p => p.Name).HasMaxLength(120).IsRequired();

            // Tenant isolation: a project is visible only to its owning user or a member of its
            // owning organization. currentUser is captured as a field access, so EF re-evaluates
            // it per query rather than baking the value into a cached plan.
            b.HasQueryFilter(p =>
                (p.OwnerType == OwnerType.User && p.OwnerUserId == currentUser.UserId) ||
                (p.OwnerType == OwnerType.Organization && p.OwnerOrganizationId != null &&
                 currentUser.OrganizationIds.Contains(p.OwnerOrganizationId.Value)));
        });

        modelBuilder.Entity<Scene>(b =>
        {
            b.Property(s => s.Name).HasMaxLength(80).IsRequired();
            b.HasOne(s => s.Project).WithMany().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(s => new { s.ProjectId, s.OrderIndex });

            // Defense in depth: Scene is always fetched through IProjectAccessService (which
            // already enforces the StreamProject filter above), but this repeats the same
            // predicate directly on Scene so a future access path can't accidentally bypass it.
            b.HasQueryFilter(s =>
                (s.Project!.OwnerType == OwnerType.User && s.Project.OwnerUserId == currentUser.UserId) ||
                (s.Project.OwnerType == OwnerType.Organization && s.Project.OwnerOrganizationId != null &&
                 currentUser.OrganizationIds.Contains(s.Project.OwnerOrganizationId.Value)));
        });

        modelBuilder.Entity<Element>(b =>
        {
            b.Property(e => e.Name).HasMaxLength(80).IsRequired();
            b.Property(e => e.Config).HasColumnType("jsonb").IsRequired();
            b.HasOne(e => e.Scene).WithMany().HasForeignKey(e => e.SceneId).OnDelete(DeleteBehavior.Cascade);
            // List<string>? maps to a native Postgres text[] column via Npgsql -- no extra
            // configuration needed, and no other module reads this column directly.
            b.HasIndex(e => new { e.SceneId, e.ZIndex });

            // Defense in depth, same reasoning as Scene's filter above -- Element is always
            // fetched through ISceneAccessService, but this closes the gap if a future access
            // path forgets to.
            b.HasQueryFilter(e =>
                (e.Scene!.Project!.OwnerType == OwnerType.User && e.Scene.Project.OwnerUserId == currentUser.UserId) ||
                (e.Scene.Project.OwnerType == OwnerType.Organization && e.Scene.Project.OwnerOrganizationId != null &&
                 currentUser.OrganizationIds.Contains(e.Scene.Project.OwnerOrganizationId.Value)));
        });

        modelBuilder.Entity<Asset>(b =>
        {
            b.Property(a => a.FileName).HasMaxLength(260).IsRequired();
            b.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            b.Property(a => a.StorageKey).HasMaxLength(400).IsRequired();

            b.HasQueryFilter(a =>
                (a.OwnerType == AssetOwnerType.User && a.OwnerUserId == currentUser.UserId) ||
                (a.OwnerType == AssetOwnerType.Organization && a.OwnerOrganizationId != null &&
                 currentUser.OrganizationIds.Contains(a.OwnerOrganizationId.Value)));
        });

        modelBuilder.Entity<OverlayToken>(b =>
        {
            b.Property(t => t.Token).HasMaxLength(64).IsRequired();
            b.HasIndex(t => t.Token).IsUnique();
            b.HasIndex(t => t.SceneId);
            b.HasIndex(t => t.ProjectId);

            // Deliberately NO tenant query filter: unlike every other tenant-owned entity here,
            // OverlayToken must remain resolvable by an anonymous caller holding a valid token
            // (that's the whole point of the capability-URL model). Authenticated issue/revoke
            // operations are instead gated by ISceneAccessService/IProjectAccessService before
            // they ever touch this table -- see OverlayService. Exactly one of SceneId/ProjectId
            // is set per row; OverlayService enforces that invariant in code (no DB check
            // constraint) since EF Core doesn't express "exactly one of" declaratively.
        });

        modelBuilder.Entity<Template>(b =>
        {
            b.Property(t => t.Name).HasMaxLength(120).IsRequired();
            b.Property(t => t.DefinitionJson).HasColumnType("jsonb").IsRequired();

            // System templates are visible to everyone; Organization templates only to members
            // of the owning org -- same shape as every other tenant filter here, just with an
            // extra always-visible branch for System scope.
            b.HasQueryFilter(t =>
                t.Scope == TemplateScope.System ||
                (t.Scope == TemplateScope.Organization && t.OrganizationId != null &&
                 currentUser.OrganizationIds.Contains(t.OrganizationId.Value)));

            b.HasData(TemplateSeedData.GetSeedTemplates());
        });

        modelBuilder.Entity<LiveSession>(b =>
        {
            b.HasIndex(s => new { s.ProjectId, s.EndedAtUtc });
            // No query filter: always reached through IProjectAccessService first (see
            // LiveControlService), same reasoning as Scene/Element's "defense in depth" comment
            // above would apply, but LiveSession rows have no direct navigation to StreamProject
            // to express it declaratively without an extra join -- not worth it for a table this
            // narrow and always-authorized-upstream.
        });

        modelBuilder.Entity<StreamingDestinationConnection>(b =>
        {
            b.Property(c => c.ExternalAccountId).HasMaxLength(200).IsRequired();
            b.Property(c => c.ExternalAccountName).HasMaxLength(200).IsRequired();
            b.Property(c => c.EncryptedAccessToken).HasMaxLength(4000).IsRequired();
            b.Property(c => c.EncryptedRefreshToken).HasMaxLength(4000);
            b.Property(c => c.Scopes).HasMaxLength(1000);
            b.HasIndex(c => new { c.OwnerUserId, c.Provider });

            b.HasQueryFilter(c =>
                (c.OwnerType == StreamingOwnerType.User && c.OwnerUserId == currentUser.UserId) ||
                (c.OwnerType == StreamingOwnerType.Organization && c.OwnerOrganizationId != null &&
                 currentUser.OrganizationIds.Contains(c.OwnerOrganizationId.Value)));
        });

        modelBuilder.Entity<AuditLogEntry>(b =>
        {
            b.Property(a => a.Action).HasMaxLength(120).IsRequired();
            b.Property(a => a.TargetType).HasMaxLength(80).IsRequired();
            b.Property(a => a.MetadataJson).HasColumnType("jsonb");
            b.HasIndex(a => new { a.OrganizationId, a.CreatedAtUtc });
            b.HasIndex(a => a.ActorUserId);

            // No query filter: rows are written by trusted server code with OrganizationId
            // already resolved, and the only read path (OrganizationService.ListAuditLogAsync)
            // does its own EnsureRoleAsync check before ever calling into this module -- see
            // IAuditQueryService's doc comment for why authorization lives one layer up.
        });
    }
}
