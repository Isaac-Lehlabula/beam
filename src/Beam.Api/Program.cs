using System.Threading.RateLimiting;
using Beam.Api.Auth;
using Beam.Api.ExceptionHandling;
using Beam.Api.HealthChecks;
using Beam.Api.Hubs;
using Beam.Infrastructure;
using Beam.Modules.AiGeneration;
using Beam.Modules.Assets;
using Beam.Modules.Audit;
using Beam.Modules.Elements;
using Beam.Modules.Identity;
using Beam.Modules.LiveControl;
using Beam.Modules.Organizations;
using Beam.Modules.Overlays;
using Beam.Modules.Projects;
using Beam.Modules.Scenes;
using Beam.Modules.StreamingDestinations;
using Beam.Modules.Templates;
using Beam.Shared.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddBeamInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();
builder.Services.AddScoped<IUserLookupService, UserLookupService>();

builder.Services
    .AddIdentityCore<BeamUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<BeamDbContext>()
    .AddSignInManager()
    // Adds "org" membership claims at sign-in (see BeamUserClaimsPrincipalFactory) -- this is
    // what makes ICurrentUserContext.OrganizationIds (and every org-scoped query filter that
    // reads it) actually reflect real membership instead of always being empty.
    .AddClaimsPrincipalFactory<BeamUserClaimsPrincipalFactory>();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "beam.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;

        // These are API endpoints, not browser login pages: return status codes instead of
        // redirecting to a (nonexistent) /Account/Login page.
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });

builder.Services.AddAuthorization();

// Overlay/control pages connect to the API directly (not through the authenticated app's
// same-origin Next.js proxy) so their SignalR connection doesn't depend on a rewrite proxy
// forwarding WebSocket upgrades correctly. They carry no cookies, so AllowAnyOrigin without
// credentials is safe -- see ARCHITECTURE.md §3/§9 and OverlayController's doc comment.
const string PublicOverlayCorsPolicy = "PublicOverlay";
builder.Services.AddCors(options => options.AddPolicy(PublicOverlayCorsPolicy, policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
// Backs StreamingDestinationService's token-at-rest encryption (Beam.Modules.StreamingDestinations).
builder.Services.AddDataProtection();
// Singleton by design: the live overrides it holds are ephemeral, per-process state
// (ARCHITECTURE.md "Design state vs. live state"), not something EF Core/the database owns.
builder.Services.AddSingleton<ILiveStateStore, InMemoryLiveStateStore>();

builder.Services.AddValidatorsFromAssembly(typeof(Beam.Modules.Projects.Contracts.CreateProjectRequest).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Beam.Modules.Scenes.Contracts.CreateSceneRequest).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Beam.Modules.Elements.Contracts.CreateElementRequest).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Beam.Modules.Templates.Contracts.InstantiateTemplateRequest).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Beam.Modules.Organizations.Contracts.CreateOrganizationRequest).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Beam.Modules.AiGeneration.Contracts.GenerateProjectRequest).Assembly);

builder.Services.AddProjectsModule();
builder.Services.AddScenesModule();
builder.Services.AddElementsModule();
builder.Services.AddAssetsModule();
builder.Services.AddOverlaysModule();
builder.Services.AddTemplatesModule();
builder.Services.AddLiveControlModule();
builder.Services.AddOrganizationsModule();
builder.Services.AddStreamingDestinationsModule(builder.Configuration);
builder.Services.AddAiGenerationModule(builder.Configuration);
builder.Services.AddAuditModule();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Rate limiting policy names. "PublicToken" guards unauthenticated, capability-token-guessable
// endpoints (the overlay state fetch) where Identity's account lockout can't help because
// there's no account. "Auth" guards login/register against credential-stuffing/enumeration,
// independent of (and in addition to) Identity's per-account lockout below. Everything else
// falls back to the generous global limiter -- see docs/SECURITY_REVIEW.md for the rationale.
const string PublicTokenRateLimitPolicy = "PublicToken";
const string AuthRateLimitPolicy = "Auth";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy(PublicTokenRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy(AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.EnsureBeamInfrastructureReadyAsync(app.Configuration);
}

app.UseExceptionHandler();

// Structured per-request log line (method, path, status, elapsed, enriched with Serilog
// properties) -- the "observability" half of Phase 12 alongside /api/health/ready below. This
// is deliberately the extent of it: logs go to Console via Serilog as before, and shipping them
// off-box is a platform/deployment concern (stdout capture by the host), not application code.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

// No policy name here deliberately: this only activates the CORS middleware so it can act on
// endpoints that opt in via [EnableCors]/.RequireCors(PublicOverlayCorsPolicy). Every other
// endpoint (the authenticated app) gets no CORS headers at all -- same-origin only.
app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LiveHub>("/hubs/live").RequireCors(PublicOverlayCorsPolicy);
app.MapHealthChecks("/api/health/ready");

app.Run();
