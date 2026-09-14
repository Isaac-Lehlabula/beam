# Beam — Phased Development Roadmap

*"Beam your stream."*

Companion to [ARCHITECTURE.md](ARCHITECTURE.md). Each phase ends with a runnable, demoable product — never a half-wired intermediate state. Work proceeds strictly in order; a phase does not start until the previous one's Definition of Done is met.

The centerpiece milestone is **Phase 3**: account → project → scene → editor → save → overlay URL → OBS Browser Source → live edit → instant update, with zero broadcast-software-specific integration code. Everything before it is necessary scaffolding; everything after it is additive value on top of an already-functional product.

---

## Phase 0 — Foundations & Scaffolding

**Goal:** the repo builds, runs, and deploys locally end-to-end with nothing user-facing yet.

- **Features:** none user-facing — this phase is pure plumbing.
- **Backend:** solution + module project skeleton (`Beam.Api`, `Beam.Modules.*`, `Beam.Shared`, `Beam.Infrastructure`) per the ARCHITECTURE §13 folder structure; Serilog wired; centralized exception handling (`IExceptionHandler` → RFC 7807 `ProblemDetails`); EF Core `DbContext` connected to PostgreSQL; `/health` endpoint.
- **Frontend:** Next.js app scaffold, Tailwind configured, minimal shell page calling `/health`.
- **Database:** initial (empty) EF Core migration; PostgreSQL running via Docker Compose.
- **APIs:** `GET /health`.
- **Tests:** solution builds clean; smoke test hitting `/health` returns 200.
- **Definition of Done:** `docker compose up` starts Postgres + API + web; the web app renders a "Beam" landing page that successfully calls the health endpoint; build and test commands run green locally.

---

## Phase 1 — Identity, Projects, Scenes (CRUD shell)

**Goal:** a real signed-up user can create a project and organize scenes — no visual design yet.

- **Features:** signup/login/logout; create/list/rename/delete a Project with a resolution (1920×1080, 1280×720, or custom); create/rename/duplicate/reorder/delete Scenes within a project.
- **Backend:** Identity module (ASP.NET Core Identity, cookie-based auth); Organizations module exists at the data-model level only (personal-owner path is what's actually used); Projects and Scenes modules; **EF Core global query filters for tenant scoping wired from day one** — this is the foundation the isolation guarantee depends on for the rest of the project.
- **Frontend:** signup/login pages, project dashboard, project detail page with scene list and drag-to-reorder.
- **Database migrations:** Users, Organizations, OrganizationMemberships, Projects, Scenes.
- **APIs:** `/api/auth/*`; `/api/projects` (CRUD); `/api/projects/{id}/scenes` (CRUD + reorder).
- **Tests:** auth flow integration tests; **tenant-isolation canary test** (user A's request can never return user B's project — this test should exist before any other module is built on top of the query-filter pattern); scene reorder unit tests.
- **Definition of Done:** a real user signs up, creates a project, adds three scenes, reorders them, logs out and back in, and finds everything exactly as left.

---

## Phase 2 — Visual Editor MVP (Text + Image)

**Goal:** the actual design surface exists.

- **Features:** open a scene in the editor; add Text and Image elements; drag/resize/rotate; layer panel with z-order; opacity; lock; show/hide; multi-select; duplicate; undo/redo; autosave + explicit save.
- **Backend:** Elements module (hybrid relational + `jsonb Config` model per ARCHITECTURE §7); Assets module (upload to object storage, server-side validation/re-encoding/thumbnailing via ImageSharp — never trust client-processed images).
- **Frontend:** shared `scene-renderer` package (ARCHITECTURE §8) driving an `<EditableCanvas>`; Zustand store; explicit invertible **Command pattern** for undo/redo (not snapshot diffing); `react-moveable` for drag/resize/rotate/snap; layer panel; element inspector (transform properties common to all elements, plus Text: font/size/weight/align/color; Image: source/fit).
- **Database migrations:** Elements, Assets.
- **APIs:** `/api/scenes/{id}/elements` (CRUD + batch geometry patch); `/api/assets` (upload, list).
- **Tests:** element CRUD integration tests; **undo/redo correctness tests** across multi-step sequences (highest-risk logic in this phase); Playwright test for a full drag → resize → save → reload round trip.
- **Definition of Done:** a user builds a scene with several text/image elements, refreshes the browser, and sees the identical layout reload from persisted state; undo/redo behaves correctly through arbitrary sequences of edits.

---

## Phase 3 — Overlay Rendering + Live Sync — 🎯 Centerpiece Milestone

**Goal:** prove the entire value proposition: **Design → Scene JSON → Overlay Renderer → Browser Source, live, with no source recreation** — in a real broadcast application.

- **Features:** "Get overlay URL" for a scene (capability token, revocable/rotatable); public overlay page that renders the scene read-only; edits made in the editor propagate to the live overlay with no reload; the overlay fully resyncs on load/reconnect (handles a browser-source refresh gracefully).
- **Backend:** Overlays module (token issuance/revocation, state-snapshot composition); `LiveHub` (SignalR) broadcasting `ElementAdded/Updated/Removed`, `SceneActivated` to the token's group; editor mutations publish to the hub immediately after persisting.
- **Frontend:** `/overlay/[token]` route — minimal layout, `scene-renderer` only, SignalR client with full-resync-then-delta logic; "Share overlay URL" panel in the editor with copy button, the project's expected width/height, guidance to disable "refresh browser when scene becomes active" in the host app, and an explicit note that the URL works in **OBS Studio, Streamlabs Desktop, XSplit, or any application with a web/Browser Source** — this is not an OBS-only feature.
- **Database migrations:** OverlayToken.
- **APIs:** `GET /api/overlay/{token}/state` (full snapshot); `LiveHub` events.
- **Tests:** integration test confirming a hub broadcast reaches every client joined to a token's group; reconnect/resync test (drop and reopen the connection, confirm correct state); **manual verification checklist against real OBS Studio** — add the generated URL as a Browser Source, edit the scene, confirm live update with zero source recreation.
- **Definition of Done:** the brief's target slice, verified against real OBS: account → project → scene → editor → save → overlay URL → OBS Browser Source → live edit → instant on-air update. **Demo this before starting Phase 4.**

---

## Phase 4 — Element Library Expansion + Editor Polish

**Goal:** broaden what can actually be designed; tighten the editing feel.

- **Features:** Video, Camera placeholder, Viewer count, Sub/Follower count (manual/static values for now — live-bound data arrives in Phase 9), QR code, Countdown timer (server-anchored end-timestamp, reconnect-stable per ARCHITECTURE §9), Progress bar/goal, News ticker, Lower third, Sponsor graphic, Guest name/title plate, basic animation presets (fade/slide in-out); snapping/alignment guides; group/ungroup.
- **Backend:** extend the element-type registry and per-type `Config` schemas; FluentValidation rules per type.
- **Frontend:** inspector panels per new element type; animation preset picker; snap-guide rendering.
- **Database:** no schema changes beyond `Config` jsonb usage (deliberately schema-flexible, per ARCHITECTURE §7).
- **APIs:** existing Elements endpoints, extended validation only.
- **Tests:** per-type config validation tests; countdown correctness test across reconnects.
- **Definition of Done:** a user builds a genuinely presentable layout (lower third + ticker + sponsor + countdown) with no code, and it renders correctly live in OBS via the Phase 3 pipeline.

---

## Phase 5 — Templates

**Goal:** a start-from-a-template flow.

- **Features:** template gallery; 2–3 seed templates (e.g. Gaming, Podcast, Just Chatting) pre-populating Starting Soon / Main / Break / Ending scenes with elements; "use template" creates a fully-editable new project.
- **Backend:** Templates module; instantiation service cloning a template's definition into real Scene/Element rows.
- **Frontend:** template gallery with preview thumbnails; "create from template" flow.
- **Database migrations:** Templates (system-scoped only; organization-scoped templates arrive in Phase 7).
- **APIs:** `GET /api/templates`; `POST /api/templates/{id}/instantiate`.
- **Tests:** instantiation-correctness test (resulting project matches the template definition); confirm instantiated elements are freely editable (no residual lock for system templates).
- **Definition of Done:** a brand-new user goes from signup to a usable, on-theme, multi-scene project in under a minute, then customizes it with the Phase 2/4 editor.

---

## Phase 6 — Live Control Panel + Live-State Layer

**Goal:** separate *designing* from *operating a live show* (ARCHITECTURE §9's design-state/live-state split, now actually exercised).

- **Features:** mobile-friendly Live Control page; start/end a Live Session; switch the active scene; show/hide a specific element live without touching the saved design; start a countdown; toggle ticker/chat visibility; trigger a predefined graphic — all reflected on the overlay immediately.
- **Backend:** LiveControl module — `LiveSession` entity plus an in-memory live-override store keyed by session id; `LiveHub` extended with control-side methods; every control action authorized server-side against the caller's role.
- **Frontend:** `/control/[token]` mobile page; overlay renderer updated to render `merge(DesignState, LiveStateOverrides)`.
- **Database migrations:** LiveSession (metadata only — overrides stay in-memory for now, per ARCHITECTURE §14).
- **APIs:** `/api/projects/{id}/live-session` (start/end); `LiveHub.TriggerAction`.
- **Tests:** authorization tests (roles are enforced, not just hidden in UI); a test proving a live override **never** mutates a persisted `Element` row.
- **Definition of Done:** during a simulated show, a producer on a phone switches scenes and toggles elements on the live overlay in real time, and afterward the saved design in the editor is byte-for-byte unchanged from before the show.

---

## Phase 7 — Organizations, Roles, Brand Kits, Template Locking

**Goal:** make the org-ready data model (present since Phase 1) actually usable.

- **Features:** create an organization; invite members; assign roles (Owner / OrgAdmin / Streamer / Producer / Designer / Moderator); brand kit (colors, fonts, logo, watermark); org-scoped templates with locked elements — members can edit only whitelisted properties, enforced server-side.
- **Backend:** Organizations module fleshed out (invitations, membership management); a central permission-checking service covering the full role matrix; template-lock enforcement inside the Elements API (a request to move/resize/delete a non-whitelisted element is rejected, not merely hidden).
- **Frontend:** org settings, member management, brand-kit editor, template lock configuration; a visibly different (disabled/lock-icon) editing state for locked elements.
- **Database migrations:** BrandKit; template-lock fields (`EditableProperties`, `SourceTemplateElementId`) already exist per ARCHITECTURE §7 — this phase is what starts enforcing them.
- **APIs:** `/api/organizations` (CRUD, invites, members); `/api/organizations/{id}/brand-kit`.
- **Tests:** the brief's exact scenario — an invited Streamer can change speaker name/title/camera source, but a raw API request to move the locked logo/watermark/sponsor graphic is rejected server-side.
- **Definition of Done:** an org admin builds a locked corporate template; an invited member personalizes the swappable fields but cannot break brand compliance, verified through both the UI and direct API calls.

---

## Phase 8 — Broadcast Software Integration Layer (OBS First)

**Goal:** a convenience/control layer on top of the already-fully-functional Browser Source workflow. Per ARCHITECTURE §4/§11, this is strictly additive — the product must keep working via plain Browser Source with this feature disconnected or absent.

- **Features:** "Connect broadcast software" — for OBS, a direct browser-to-`localhost` OBS WebSocket connection (credentials never touch our servers); once connected: list scenes, switch scenes, auto-create/update the Beam browser source, show/hide sources, read streaming/recording status. UI adapts to the connected provider's `BroadcastSoftwareCapabilities` rather than assuming OBS-specific features are universal.
- **Backend:** BroadcastIntegration module defining `IBroadcastSoftwareIntegration` + the capability-flag contract + `ObsIntegration`; explicitly **no server-side storage of OBS credentials**.
- **Frontend:** the "Connect OBS" flow runs entirely client-side, in a browser tab on the same machine as OBS; live connection status; OBS scene list synced into the UI; one-click "create/update browser source in OBS" using the existing overlay URL from Phase 3.
- **Database:** none required for credentials (by design); optionally a non-secret `PreferredBroadcastProvider` field on Project.
- **APIs:** none new for the OBS connection itself (browser ↔ OBS directly); reuses the existing Overlays snapshot API to supply the source URL.
- **Tests:** adapter unit tests against a mocked OBS WebSocket server; capability-flag UI test (controls correctly hide for a lower-capability mock provider); manual checklist against real OBS (create source, switch scene, read status).
- **Definition of Done:** with real OBS running locally, a user clicks "Connect OBS," sees their actual OBS scenes, and can switch scenes and auto-create the browser source from within Beam — while the Phase 3 manual-paste workflow continues to work unchanged if OBS is never connected.

---

## Phase 9 — Streaming Destination Integration Layer

**Goal:** real chat and viewer/sub-count data flowing into elements, from cloud platform APIs — a completely different integration shape from Phase 8.

- **Features:** connect a YouTube and/or Twitch account via OAuth; Chat element shows real messages; Viewer/Sub-count elements show real live numbers; event-stream groundwork for Phase 10's alerts.
- **Backend:** StreamingDestinations module defining `IStreamingPlatformIntegration` + Twitch/YouTube adapters; encrypted-at-rest token storage; normalized DTOs (`ChatMessage`, `ViewerMetrics`) consumed generically by the Elements module.
- **Frontend:** "Connect channel" settings flow (OAuth redirect); Chat/ViewerCount/SubCount elements gain a live-data-source picker alongside their existing manual-value mode.
- **Database migrations:** StreamingDestinationConnection (encrypted OAuth tokens).
- **APIs:** `/api/integrations/streaming/{provider}/connect`; provider-specific webhook/poll endpoints as each API requires.
- **Tests:** token-encryption round-trip test; adapter tests against sandbox/mocked provider responses; DTO-normalization test (the Chat element renders identically regardless of which provider is connected).
- **Definition of Done:** a real connected Twitch or YouTube channel's live chat and viewer count appear correctly on the overlay during an actual broadcast test.

---

## Phase 10 — Alerts, Donations, Remaining Element Types

**Goal:** round out the element library with event-driven graphics.

- **Features:** Alert element (new follower/sub/donation triggers a configurable animated graphic); donation-alert specifics; Poll, Scoreboard, Image carousel, Now-playing, richer ticker behavior; Website/browser embed element with strict XSS-safe sandboxing (locked-down `iframe`, no arbitrary injected script from user-provided HTML/URLs).
- **Backend:** event ingestion from StreamingDestinations adapters (webhook/EventSub-style where the provider supports it) feeding `LiveHub` alert broadcasts; embed-element URL validation/allowlisting.
- **Frontend:** alert designer (trigger condition → animation/graphic mapping); remaining inspector panels.
- **Database:** minor `Config` schema additions only.
- **APIs:** provider webhook endpoints; `LiveHub` extended with Alert events.
- **Tests:** **XSS-sandboxing test for the embed element** (this is explicitly called out as a risk in the original brief and deserves a dedicated test, not just review); alert-trigger-to-overlay latency/correctness test.
- **Definition of Done:** a test donation/follow event on a connected channel triggers the configured alert animation on the live overlay within a couple of seconds, and the embed element cannot execute injected script against the page.

---

## Phase 11 — AI-Assisted Scene Generation

**Goal:** prompt → starter design, fully editable afterward — explicitly not required before this point.

- **Features:** a user describes their show in natural language; the system proposes a theme (colors/fonts) and a matching set of scenes/elements; the result opens directly in the standard editor — there is no separate "AI mode" data model.
- **Backend:** a generation service that maps a prompt to the **same Template-instantiation pipeline built in Phase 5** (the AI produces a Template-shaped payload and reuses existing instantiation code, rather than a parallel rendering path).
- **Frontend:** a "Describe your show" entry point alongside the template gallery; the generated result opens in the standard editor, indistinguishable from a hand-built project.
- **Database:** none beyond the existing Template/Project/Scene/Element tables.
- **APIs:** `POST /api/ai/generate-project` (prompt in, project id out).
- **Tests:** schema-validation test that generated output always conforms to a valid Template shape (guards the domain against malformed AI output); golden-prompt regression tests.
- **Definition of Done:** a prompt like the brief's "football podcast, black and gold, three presenters" example produces a themed, multi-scene, immediately-editable project with zero manual scene creation.

---

## Phase 12 — Hardening & Scale-Readiness

**Goal:** production-grade non-functionals, addressing every security concern raised in the original brief explicitly.

- **Features:** rate limiting (especially the public overlay/control token endpoints); audit logging for live-control and org-admin actions; observability (structured log shipping, health/metrics dashboards); horizontal-scale readiness (Redis SignalR backplane + moving live-session overrides to Redis — enabled only if/when actually needed); billing/plan gating if applicable; broader automated test coverage; a dedicated security review pass.
- **Backend / Frontend / Database / APIs:** incremental hardening across existing modules — this phase intentionally adds no new user-facing surface.
- **Tests:** load test on `LiveHub` fan-out under many concurrent overlay/control connections; targeted security tests against each item below.
- **Definition of Done:** a documented pass against every security bullet from the original brief — authentication, authorization, organization isolation, file uploads, image/video uploads, browser-source access, public overlay URLs, WebSockets/SignalR, secrets/API tokens, third-party OAuth tokens, XSS from custom text/HTML, rate limiting — each either verified-mitigated or explicitly tracked as a follow-up with rationale.
