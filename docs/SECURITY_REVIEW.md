# Beam — Security & Hardening Review (Phase 12)

This is the Definition of Done deliverable for Roadmap Phase 12: a pass against every
security-relevant item from the original brief, each marked **Mitigated** (with the code that
does it) or **Tracked follow-up** (with the rationale for deferring it). It also records what
Phase 12 added: rate limiting, audit logging, and basic observability.

## What Phase 12 added

- **Rate limiting** (`src/Beam.Api/Program.cs`, `AddRateLimiter`): a generous global per-IP
  fixed-window limiter (300 req/min) covers every endpoint by default. Two tighter named
  policies layer on top:
  - `PublicToken` (60 req/min/IP) on `OverlayController` — the one endpoint an anonymous caller
    can hit at all, guarded only by a capability token.
  - `Auth` (10 req/min/IP) on `POST /api/auth/login` and `POST /api/auth/register` —
    credential-stuffing/enumeration protection independent of (and in addition to) ASP.NET Core
    Identity's own per-account lockout (`lockoutOnFailure: true` in `AuthController.Login`).
  Verified manually: 65 rapid requests to `/api/overlay/{bogus-token}/state` returned `404` for
  the first 60 and `429 Too Many Requests` for the rest.
- **Audit logging** (`Beam.Modules.Audit`): every live-control action
  (`live_session.start/end/switch_scene/element_visibility`) and org-admin action
  (`organization.member.add/role_update/remove`, `organization.brand_kit.update`) writes an
  `AuditLogEntry` row (actor, organization, action, target, JSON metadata). Org owners/admins
  can read their organization's trail via `GET /api/organizations/{id}/audit-log` — no new
  frontend surface was added for it (Phase 12 is explicitly non-user-facing per the roadmap);
  it exists for support/incident-response use today. Verified end-to-end in the browser: a
  brand-kit update produced a matching audit row visible through the endpoint.
- **Observability**: `app.UseSerilogRequestLogging()` adds a structured per-request log line
  (method, path, status, elapsed). `GET /api/health/ready` (ASP.NET Core `HealthChecks`,
  `DatabaseHealthCheck`) reports real DB connectivity, distinct from the existing
  `GET /api/health` liveness ping which only proves the process is up. "Structured log shipping"
  itself is not new code: Serilog already emits structured (property-carrying, not just
  interpolated-string) events to Console, which is the standard hand-off point for a platform's
  own log collector (container stdout capture, App Service log stream, etc.) — see *Tracked
  follow-ups* below for what's deliberately not done yet.

## Checklist

### Authentication
**Mitigated.** ASP.NET Core Identity with cookie auth (`beam.auth`, `HttpOnly`,
`SameSite=Lax`, `SecurePolicy=SameAsRequest`), 14-day sliding expiration
(`Program.cs`). Passwords: Identity's default hasher (PBKDF2), `RequiredLength = 8`.
Login/registration return status codes, not redirects (`OnRedirectToLogin` /
`OnRedirectToAccessDenied` overridden — this is an API, not a page app). Login failures don't
distinguish "no such user" from "wrong password" (`AuthController.Login`). Per-account lockout
on repeated failures (`PasswordSignInAsync(..., lockoutOnFailure: true)`) plus the new `Auth`
rate-limit policy above.

### Authorization
**Mitigated.** Every authenticated controller carries `[Authorize]` at the class level;
`OverlayController` is the sole `[AllowAnonymous]` exception, by design (see below). Role checks
for organization-admin actions go through `IOrganizationAccessService.EnsureRoleAsync`
(`OrganizationService`), never ad hoc.

### Organization isolation (multi-tenancy)
**Mitigated.** EF Core global query filters on every tenant-owned entity
(`BeamDbContext.OnModelCreating`) key off `ICurrentUserContext.OrganizationIds`/`UserId`,
captured as a field reference so filters re-evaluate per query rather than baking a value into a
cached plan. `Element`/`Scene` carry the same filter as their owning `StreamProject` a second
time ("defense in depth" — see the code comments) even though every real access path already
goes through `IProjectAccessService`/`ISceneAccessService` first. Regression-tested:
`ProjectTenantIsolationTests` (a user cannot see another user's personal project; a non-member
cannot see an org's project). `AuditLoggingTests` adds the equivalent check for the new
audit-log read path (a non-admin member gets `ForbiddenAccessException`).

### File uploads (images)
**Mitigated.** `AssetService.UploadImageAsync` re-encodes every uploaded image via
ImageSharp rather than trusting/storing the original bytes — this strips embedded scripts/exif
payloads and rejects anything that isn't decodable as an image, closing the classic
"polyglot file disguised as an image" attack. Size-limited server-side.

### File uploads (video)
**Tracked follow-up, disclosed at the time it was built (Phase 4).**
`AssetService.UploadVideoAsync` sniffs the magic bytes (MP4 `ftyp` box / WebM EBML header) to
reject non-video uploads and enforces a 100MB cap
(`[RequestSizeLimit(100*1024*1024)]` on `AssetsController.Upload`), but — unlike images — the
file is **not** re-encoded before storage, because no video-processing library is in the stack.
Rationale for deferring: video re-encoding is a substantial dependency (ffmpeg or similar) and
CPU/queue-infrastructure decision that didn't fit this pass. Risk is bounded by: (1) videos are
only ever rendered inside a sandboxed `<video>` tag, never executed; (2) uploads are
tenant-scoped like every other asset, so this is not a public upload surface.

### Browser-source access (OBS integration)
**Mitigated by design, not by server-side control.** `ObsWebSocketClient`/`ObsIntegration`
(`web/src/lib/obs/`) run entirely in the browser and talk directly to `localhost`'s OBS
WebSocket server — the Beam backend is never in this path and never sees the OBS password. The
password is entered into a form and kept in memory only; only host/port are persisted to
`localStorage` (`ConnectObsPanel.tsx`). This matches ARCHITECTURE.md §3/§9's threat model: OBS's
embedded Browser Source is treated as untrusted-for-credentials by construction.

### Public overlay URLs (capability tokens)
**Mitigated.** `OverlayToken` rows carry a random 64-char token and are the *only* entities in
`BeamDbContext` with no tenant query filter, on purpose — the token itself is the credential for
an anonymous caller (OBS's embedded browser can't complete a real login). Issuing/rotating a
token is authenticated and access-checked (`OverlayTokensController` is `[Authorize]`, routes
through `IProjectAccessService`/`ISceneAccessService`). The read side
(`OverlayController.GetState`) is now covered by the `PublicToken` rate-limit policy added in
this phase, which bounds brute-force token guessing (token space is large enough that 60
guesses/minute/IP is not a practical attack, but it's a real backstop against scraping/DoS of
the endpoint itself). CORS is opened only for this specific route
(`[EnableCors("PublicOverlay")]`), and the connection carries no cookies
(`withCredentials: false` on the SignalR client), so the wildcard-origin CORS policy can't be
abused to read authenticated data.

### WebSockets / SignalR
**Mitigated.** `LiveHub` requires the same `PublicOverlay` CORS policy as the overlay REST
endpoint and carries no auth cookie — group membership (`LiveGroupNames.ForScene`/`ForProject`)
is the only thing scoping what a connection receives, and a connection can only join a group by
presenting a valid capability token to `IOverlayService` first (verified server-side, not
trusted from the client). No production credentials ever cross this channel.
**Tracked follow-up:** SignalR connection/message-rate limiting is not implemented — ASP.NET
Core's `RateLimiter` middleware only governs HTTP requests, not the WebSocket message stream
after a hub connection is established. For a single-instance MVP this is an acceptable gap
(the connection count itself is naturally bounded by browser sources actually configured); flag
for revisit if/when horizontal scaling (see below) is undertaken.

### Secrets / API tokens
**Mitigated.** No secret is ever hardcoded: connection strings, storage credentials,
Twitch/YouTube OAuth client secrets, and the Anthropic API key are all `IConfiguration`-sourced
(`appsettings.json` ships empty placeholders; real values are expected via environment/secret
manager in every real deployment). OBS WebSocket passwords never reach the server (see above).

### Third-party OAuth tokens (Twitch / YouTube)
**Mitigated.** `StreamingDestinationConnection.EncryptedAccessToken`/`EncryptedRefreshToken`
are encrypted at rest via ASP.NET Core Data Protection (`IDataProtector`, purpose string
`"Beam.StreamingDestinationTokens.v1"` — `StreamingDestinationService`), decrypted only inside
that service, on demand, immediately before an outbound API call. CSRF state for the OAuth
`state` parameter is generated and validated entirely client-side
(`sessionStorage`, `callback/page.tsx`) — the backend's `OAuthCallbackRequest` deliberately has
no `state` field, so a forged callback POST straight to the API (bypassing the frontend's state
check) still can't complete without a code the attacker doesn't have. Config-value validation
bug found and fixed in this phase's predecessor (Phase 9): `RequireClientId()` originally used
`?? throw` which only catches `null`, not empty string — `appsettings.json`'s empty-string
placeholder silently produced a broken `client_id=` OAuth URL. Fixed to
`string.IsNullOrWhiteSpace(...)`, regression-tested (`StreamingDestinationTests`,
`[InlineData(null)] [InlineData("")] [InlineData("   ")]`).

### XSS from custom text/HTML
**Mitigated for the general case; one deliberate, contained exception.** React escapes all
user-authored text content by default (element `content`, `Label`, ticker text, etc. — none of
it is rendered via `dangerouslySetInnerHTML`). The one place raw external content is rendered is
`BrowserEmbedElementConfig` (`BrowserEmbedRenderer` in `ExpandedElementRenderers.tsx`): it's an
`<iframe>`, not injected markup, `https://`-only (enforced server-side by
`BrowserEmbedElementConfigValidator` via `Uri.TryCreate` + scheme check, not just client-side),
and sandboxed with `sandbox="allow-scripts allow-popups"` — deliberately **without**
`allow-same-origin`, so embedded content can't read/write the parent page's storage or cookies
even if the embedded URL itself is malicious.

### Rate limiting
**Mitigated** — see "What Phase 12 added" above.

## Tracked follow-ups (explicitly deferred, with rationale)

- **Video re-encoding on upload.** See "File uploads (video)" above.
- **SignalR message-level rate limiting.** See "WebSockets / SignalR" above.
- **Horizontal-scale readiness (Redis SignalR backplane + Redis-backed `ILiveStateStore`).**
  Not implemented. `InMemoryLiveStateStore` and SignalR's default in-memory backplane are both
  explicitly single-instance (see `ILiveStateStore`'s doc comment and ARCHITECTURE.md §14) —
  this was a known, deliberate MVP trade-off, not an oversight. `ILiveStateStore` is already an
  interface specifically so a Redis-backed implementation is a drop-in swap; SignalR's own
  `AddStackExchangeRedis()` extension does the same for the hub backplane. Neither is worth
  adding until there's an actual second instance to synchronize — the roadmap phrases this same
  way ("enabled only if/when actually needed").
- **Billing / plan gating.** Not applicable — no billing model exists yet in the product.
- **Dedicated log-shipping sink** (Seq / ELK / Datadog / Application Insights). Not added:
  no concrete observability backend has been chosen yet, and Serilog's structured Console
  output is forward-compatible with adding one later (swap/add a `WriteTo` sink, no call-site
  changes needed anywhere in the app).
- **Audit-log frontend UI.** Not built — Phase 12 is explicitly scoped as adding no new
  user-facing surface (per ROADMAP.md). The read endpoint exists for direct/support use; a
  settings-page UI over it is a small, self-contained follow-up whenever it's wanted.
