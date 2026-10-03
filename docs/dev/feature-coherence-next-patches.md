# Feature Coherence Implementation Backlog

This branch establishes the truth table, maturity-first README draft, security documentation split, coherence CI scripts, and first concrete security utility tests. The remaining work below should be implemented as small reviewable patches.

## 1. Keep the maturity-first README current

Status: completed and revalidated 2026-10-02. The maturity-first landing
README was previously introduced, then the root README regrew into an
unqualified feature catalogue. README.md now matches the reviewed concise
landing page; setup and feature maturity links are prominent, and the separate
feature inventory remains authoritative. The copy check prevents drift.

Validation:

```bash
bash scripts/audit-feature-coherence.sh
bash scripts/audit-readme-maturity-draft.sh
bash scripts/audit-roadmap-claims.sh
cmp README.md README.maturity.md
```

Acceptance criteria:

- `README.md` points to `FEATURE_INVENTORY.md` and `docs/status.md`.
- The README no longer markets roadmap-only security systems as implemented.
- The README distinguishes the core baseline, experimental extensions, roadmap-only security claims, and the separate slskr project.

## 2. Wire BindExposureAnalyzer into Program.cs

Status: complete for direct startup wiring. `Program.cs` now passes analyzed web listener exposure to `HardeningValidator`.

Target behavior:

- Stop deriving remote exposure from whether a web port is enabled.
- Classify the actual configured web bind address/socket.
- Pass `BindExposureAnalyzer.IsRemoteReachable(exposure)` into `HardeningValidator.Validate(...)`. Done.
- Log the computed exposure at startup for operator/debug visibility.

Acceptance criteria:

- Auth-disabled + loopback-only bind does not fail as remote exposure.
- Auth-disabled + Unix-socket-only bind does not fail as remote exposure.
- Auth-disabled + wildcard/private/public/unknown TCP bind still fails or warns according to existing `HardeningValidator` policy.
- Startup-level tests cover the matrix.

## 3. Add HardeningValidator startup matrix tests

Status: complete at the validator boundary with `BindExposureAnalyzer.AnalyzeWebBinding(...)`; full host construction coverage can still be added if startup regressions appear.

Required cases:

- Auth disabled + `127.0.0.1` + enforce => allowed. Done.
- Auth disabled + `localhost` + enforce => allowed. Done.
- Auth disabled + Unix socket only + enforce => allowed. Done.
- Auth disabled + `0.0.0.0` + enforce + no `AllowRemoteNoAuth` => fail. Done.
- Auth disabled + `192.168.x.x` + enforce + no `AllowRemoteNoAuth` => fail. Done.
- Auth disabled + `::` + enforce + no `AllowRemoteNoAuth` => fail. Done.
- Auth disabled + `AllowRemoteNoAuth` + no CIDRs => fail. Done.
- Auth disabled + `AllowRemoteNoAuth` + CIDRs => allowed. Done.

## 4. Audit PathGuard call sites

Status: Complete (2026-10-03). The audit covered the user/server-derived file
paths that list, read, write, move, or delete files and added regressions for
symlink escapes.

Search targets:

```bash
rg "Path\.Combine|Path\.GetFullPath|File\.Delete|FileStream|OpenRead|OpenWrite|Move\(|Copy\(|Delete\(" src/slskd src/slskdN.VpnAgent tests
rg "NormalizeAndValidate|NormalizeAbsolutePathWithinRoots|PathGuard" src tests
```

Acceptance criteria:

- Peer/server-supplied paths go through `PathGuard` or have a documented reason not to.
- Delete-file, streaming, downloads, browse, relay, and share paths are explicitly covered.
- Any bypass gets a TODO tied to an issue or a test.

Coverage:

- Files API listing and deletion validate decoded paths against configured roots;
  `FileService` resolves current symlink targets and skips reparse points.
- Library Items fallback browsing skips reparse points and validates each file
  against its configured share/download roots before checking metadata or
  registering a content ID.
- Streaming and player tag operations start from `ContentLocator`'s rooted
  physical-file resolver. Mesh content serving uses `ShareService.ResolveFileAsync`;
  relay reads and local uploads use the same current-root share resolution.
  Mesh proof-of-possession chunk reads revalidate cached FLAC-key paths against
  configured share roots before opening them.
- Incomplete downloads, completed destinations, and persisted transfer removal
  revalidate paths against their configured roots. Pod downloads validate the
  peer filename before directory creation or fetching bytes, then use
  `SecureFileWriter`. Share backfills validate generated paths and use the
  same safe writer; relay destinations remain rooted and size-limited.
- `ShareScanner` skips reparse points. `ShareService` revalidates cached local
  share paths at use time and requests a rescan when an indexed path has moved
  outside configured shares.
- Remaining direct `FindFileInfo` calls in relay, backfill, and moderation are
  index/metadata checks only. The physical read is delegated to the guarded
  resolver, the destination to `DownloadService`, or no filesystem operation
  occurs.
- `scripts/check-path-containment.sh` asserts the central call sites, and
  focused symlink regressions cover browsing, streaming, deletion, downloads,
  share resolution, pod downloads, and mesh proof-of-possession chunk reads.

## 5. Audit ContentSafety call sites and policy

Status: Complete for standard Soulseek downloads, pod/mesh search downloads,
HTTP collection backfills, relay imports, multi-source output, and the
VirtualSoulfind mesh-transfer receiver. Every completed output is checked after
its write stream closes and before success is reported; alternate receivers
keep it in a private staging path until accepted. The standard Soulseek pipeline
persists failed transfer and request state when policy rejects a file.

Policy:

- Dangerous executable signatures disguised as another file type are always
  rejected while content scanning is active. `BlockExecutables` also rejects
  executable signatures under executable extensions.
- Failed signature verification is rejected. A known-type mismatch warning is
  quarantined when `QuarantineSuspicious` is true; otherwise it is logged and
  allowed to the normal destination.
- Rejected files move to the configured quarantine directory by default. An
  empty setting means `<directories.downloads>/.quarantine`; relative paths are
  resolved under the downloads root. When quarantine is disabled, rejected
  files are removed from the incomplete directory.
- Disabling `VerifyMagicBytes` skips format-mismatch enforcement. Executable
  signatures are still checked when `BlockExecutables` is enabled. Disabling
  both settings turns off signature scanning.

Shared implementation: `ContentSafety.InspectAndApplyPolicyAsync` performs the
same enabled check, signature assessment, warning/rejection decision, and
quarantine/removal disposition for each receiver. Collection backfills preserve
the manifest filename when available and use `.bin` when no true extension is
known, avoiding a false MP3 claim based only on `MediaKind`.

Acceptance criteria:

- All receive paths call the shared post-write policy before success.
- Mismatch handling is explicit: quarantine warnings by default, log and allow
  them when quarantine is disabled, and reject failed verification.
- Dangerous executable masquerading as media is rejected and quarantined by
  default, even when ordinary magic-byte matching is disabled.
- `DownloadServiceTests.EnqueueAsync_ContentSafetyQuarantinesExecutableAndMismatchedFiles`
  exercises the real output factory, SQLite transfer/request records, and local
  filesystem disposition with magic-byte checking both enabled and disabled.
- Focused regressions cover pod HTTP status and quarantine, collection backfill
  failure counts, relay notification quarantine, multi-source sequential
  failover, and the VirtualSoulfind receiver.
- Existing limits, hash checks, SSRF controls, and path guards remain active.
- `ContentSafetyTests.PublishStagedFile_CreatesMissingNestedDestinationDirectory`
  verifies publication creates a missing nested target directory; the full
  two-node mesh search/download test verifies the same path through a real
  receiver.

Validation on 2026-10-03: `dotnet test` passed (74 application, 5,391 unit,
285 integration), `./bin/lint`, path containment, release-note preview,
local-identity, feature-coherence, README-maturity, roadmap-claim, and whitespace
checks passed.

## 6. Remove or hide HashFromAudioFileEnabled

Status: complete for current exposure. Direct public CLI/env exposure was
removed, `HardeningValidator` now fails startup whenever this unsupported
option is true, and the SongID capability reporter marks the flag `broken` and
unavailable.

Preferred resolution:

- Remove public docs/config exposure for `HashFromAudioFileEnabled`, or rename to `ExperimentalHashFromAudioFileEnabled`.
- Add a runtime capability check if the feature is kept. Done via `/api/v0/songid/capabilities`.
- Ensure SongID docs/UI do not imply local audio hashing works unless capability is present. Docs now point to runtime capabilities.

Acceptance criteria:

- Normal users cannot enable a known-unavailable feature casually.
- README and config examples do not market unavailable local audio hashing as working.

## 7. Split Program.cs service registration

Status: in progress. SongID service registration moved into
`Bootstrap/SongIdServiceCollectionExtensions.cs`, and the large experimental
feature graph (multi-source, VirtualSoulfind, MediaCore, pods, mesh/DHT,
wishlist/source feeds, relay, FTP, AudioCore metadata, notifications) moved out
of `Program.cs` into `Bootstrap/ExperimentalFeatureGraphServiceCollectionExtensions.cs`.
User notes, collections/sharing, identity/friends, and Solid/WebID registration
also moved into `Bootstrap/UserDataServiceCollectionExtensions.cs`.
Core database context setup, event/telemetry registration, app-owned
integrations, messaging/search/share/user services, transfer services, and
source ranking moved into `Bootstrap/CoreApplicationServiceCollectionExtensions.cs`.
Startup options, feature gates, managed state, HTTP clients, Soulseek client
construction, and the `IApplication` hosted-service wrapper moved into
`Bootstrap/ApplicationHostServiceCollectionExtensions.cs`.
ASP.NET service registration for CORS, runtime metrics, data protection,
authentication/authorization, moderation, controllers, SignalR, health checks,
API versioning, rate limiting, and Swagger moved into
`Bootstrap/WebServiceCollectionExtensions.cs`.
ASP.NET request-pipeline setup moved into
`Bootstrap/WebApplicationPipelineExtensions.cs`.
The top-level runtime DI composition list moved into
`Bootstrap/RuntimeServiceCollectionExtensions.cs`.
Wishlist/source feeds, transfer automation, relay, FTP, AudioCore metadata,
SongID, discovery graph, and notification registration moved out of the broad
experimental graph into `Bootstrap/IntegrationAndMediaServiceCollectionExtensions.cs`.
Multi-source transfer, swarm, tracing, warm-cache, playback-priority, and job
manifest registrations moved out of the broad experimental graph into
`Bootstrap/MultiSourceFeatureServiceCollectionExtensions.cs`.
VirtualSoulfind capture, shadow index, scene, disaster-mode, bridge, v2
provider/backend, reconciliation, and processing registrations moved out of the
broad experimental graph into `Bootstrap/VirtualSoulfindServiceCollectionExtensions.cs`.
Backfill, mesh hash-sync, source discovery, rescue, accelerated download,
content verification, peer metrics, and chunk scheduler registrations moved out
of the broad experimental graph into
`Bootstrap/TransferDiscoveryServiceCollectionExtensions.cs`.
MediaCore, PodCore, content-domain provider, and peer-reputation registrations
moved out of the broad experimental graph into
`Bootstrap/MediaCorePodServiceCollectionExtensions.cs`.
Mesh, DHT, overlay, transport, realm, governance, gossip, social-federation,
privacy, NAT, and service-fabric registrations moved out of the broad
experimental graph into `Bootstrap/ExperimentalMeshServiceCollectionExtensions.cs`.
MediaCore publisher, capability bridge, and DHT rendezvous registrations moved
out of the broad experimental graph into
`Bootstrap/CapabilitiesAndRendezvousServiceCollectionExtensions.cs`.
E2E hosted-service tracing and host startup timeout/concurrency options moved
out of `Program.cs` into `Bootstrap/HostDiagnosticsServiceCollectionExtensions.cs`.
Post-build startup tasks, including database migration, optional audio
reanalyze migration, and forced construction of event-subscriber integrations,
moved out of `Program.cs` into `Bootstrap/ApplicationStartupTaskExtensions.cs`.
Web listener/Kestrel configuration moved out of `Program.cs` into
`Bootstrap/WebHostConfigurationExtensions.cs`.
Application run/lifecycle hooks, E2E server probes, and LAN discovery
advertising start/stop moved out of `Program.cs` into
`Bootstrap/ApplicationRunExtensions.cs`.
Configuration compatibility warning parsing moved out of `Program.cs` into
`Configuration/ConfigurationCompatibilityWarnings.cs`.
Expected Soulseek network exception classification moved out of `Program.cs`
into `Soulseek/SoulseekNetworkExceptionClassifier.cs`.
Initial Soulseek client option construction moved out of `Program.cs` into
`Soulseek/SoulseekClientOptionsFactory.cs`.
App-relative path resolution moved out of `Program.cs` into
`Configuration/AppPathResolver.cs`, and web HTML asset rewrite rule construction
moved into `Bootstrap/WebHtmlRewriteRules.cs`.
Antiforgery stale-cookie recovery, request-cookie stripping, and stale-token
classification moved out of `Program.cs` into
`Core/Security/AntiforgeryCookieRecovery.cs`.
Startup configuration provider composition moved out of `Program.cs` into
`Configuration/SlskdConfigurationBuilderExtensions.cs`.
Startup filesystem checks, missing configuration-file recreation, and generated
certificate export moved out of `Program.cs` into `Bootstrap/StartupFileSystem.cs`.
QUIC overlay client/server construction and standalone UDP overlay selection
moved out of `Program.cs` into `Mesh/Overlay/QuicOverlayFactory.cs`.
Serilog startup configuration moved out of `Program.cs` into
`Bootstrap/StartupLogging.cs`, and shutdown/unobserved-exception telemetry moved
into `Bootstrap/StartupShutdownTelemetry.cs` while `Program` retains the public
log event and buffer surface.
CLI help output, environment-variable listing, and startup logo rendering moved
out of `Program.cs` into `Bootstrap/StartupConsoleOutput.cs`.
SQLite provider initialization and threading fail-fast validation moved out of
`Program.cs` into `Bootstrap/StartupSqlite.cs`.
Runtime version, canary/development flags, and executable-path calculation moved
out of `Program.cs` into `Bootstrap/ApplicationRuntimeInfo.cs` while preserving
the public Program compatibility surface.
Startup mutex-name construction and unobserved-task exception classification
moved out of `Program.cs` into `Bootstrap/StartupSingleInstance.cs` and
`Bootstrap/StartupExceptionClassifier.cs` while preserving Program
compatibility wrappers.
Owned physical file provider construction moved out of `Program.cs` into
`Bootstrap/StartupFileSystem.cs` while preserving the Program compatibility
wrapper.
Web pipeline setup now calls extracted web rewrite, antiforgery recovery, and
startup file-system helpers directly. Experimental mesh service registration now
calls the QUIC overlay factory directly instead of routing through Program
compatibility wrappers.
Primitive startup command-mode handling for version/help/env output,
certificate generation, and secret generation moved out of `Program.cs` into
`Bootstrap/StartupCommandMode.cs`.
Startup application-directory default resolution and default directory
validation moved out of `Program.cs` into
`Bootstrap/StartupApplicationDirectories.cs`.
Startup configuration provider loading, binding, raw security-section
diagnostics, and validation moved out of `Program.cs` into
`Bootstrap/StartupConfiguration.cs`.
Configured startup identity, system, directory, compatibility-warning, and
logging-target diagnostics moved out of `Program.cs` into
`Bootstrap/StartupDiagnostics.cs`.
ASP.NET hardening validation, builder configuration, service registration, DI
build, pipeline setup, no-start handling, and run lifecycle moved out of
`Program.cs` into `Bootstrap/StartupWebApplicationRunner.cs`.
Production call sites now use extracted path, Soulseek option, QUIC data-plane,
and antiforgery helpers directly instead of routing through Program
compatibility wrappers.
Focused tests now exercise the extracted helpers directly, and redundant
test-only Program compatibility wrappers for paths, rewrite rules, Soulseek
options, startup exception classification, expected Soulseek network exception
classification, and QUIC standalone-socket selection have been removed.
Leftover dead Program fields and wrappers from earlier helper extractions were
removed, while command-line argument population remains in `Program.cs` because
the command-line library binds static `[Argument]` properties from that context.
Startup application-directory resolution, single-instance mutex acquisition,
configuration-file defaulting, and default directory validation moved out of
`Program.cs` into `Bootstrap/StartupApplicationDirectories.cs`.
Startup configuration load/validation exception handling moved out of
`Program.cs` into `Bootstrap/StartupConfiguration.cs`.
Startup command-mode console output, certificate generation, and startup logo
rendering now call extracted bootstrap helpers directly instead of routing
through Program wrappers.
Startup SQLite initialization and missing-config recreation now call extracted
bootstrap helpers directly instead of routing through Program wrappers.
Startup logging configuration and shutdown telemetry installation now call
extracted bootstrap helpers directly instead of routing through Program
wrappers.
The remaining antiforgery Program wrappers were removed after the MVC CSRF
filter and focused tests moved to `AntiforgeryCookieRecovery` directly.
Transfer service registration now belongs to
`Bootstrap/TransfersServiceCollectionExtensions.cs`. The core graph delegates
download, upload, transfer, file-service, and auto-replace registration to that
module. Transfer hosted-service descriptors are added from the integration
graph at their previous position, preserving hosted-service startup order.
`IAutoReplaceService` now has one registration owner, protected by a descriptor
count test.

Target modules:

- `AddSlskdCore(...)`
- `AddSlskdCoreApplicationServices(...)`. Implemented for app persistence,
  messaging/search/share/user, transfers, and source ranking.
- `AddSlskdApplicationHost(...)`. Implemented for startup options, state,
  HTTP clients, Soulseek client, and `IApplication` hosting.
- `AddSlskdWebServices(...)`. Implemented for ASP.NET service registration.
- `UseSlskdWebPipeline(...)`. Implemented for ASP.NET middleware and endpoint
  registration.
- `AddSlskdRuntimeServices(...)`. Implemented as the top-level runtime service
  composition wrapper.
- `AddSlskdIntegrationAndMediaServices(...)`. Implemented for integration and
  media-adjacent registrations formerly at the tail of the experimental graph.
- `AddSlskdMultiSourceFeatureServices(...)`. Implemented for multi-source
  transfer, swarm, tracing, warm-cache, playback-priority, and job-manifest
  registrations.
- `AddSlskdVirtualSoulfindServices(...)`. Implemented for VirtualSoulfind
  capture, shadow-index, scene, disaster-mode, bridge, v2 provider/backend,
  reconciliation, and processing registrations.
- `AddSlskdTransferDiscoveryServices(...)`. Implemented for backfill, mesh
  hash sync, source discovery, rescue, accelerated download, content
  verification, peer metrics, and chunk scheduling registrations.
- `AddSlskdMediaCorePodServices(...)`. Implemented for MediaCore, PodCore,
  content-domain provider, and peer-reputation registrations.
- `AddSlskdExperimentalMeshServices(...)`. Implemented for mesh, DHT, overlay,
  transport, realm, governance, gossip, social-federation, privacy, NAT, and
  service-fabric registrations.
- `AddSlskdCapabilitiesAndRendezvousServices(...)`. Implemented for MediaCore
  publishing, capability bridge, and DHT rendezvous registrations.
- `AddSlskdHostDiagnostics(...)`. Implemented for E2E hosted-service tracing
  and host startup timeout/concurrency options.
- `RunSlskdStartupTasks(...)`. Implemented for database migrations, optional
  audio reanalysis, and event-subscriber integration construction.
- `ConfigureSlskdWebHost(...)`. Implemented for web listener/Kestrel setup.
- `RunSlskdApplication(...)`. Implemented for application run/lifecycle hooks,
  E2E server probes, and LAN discovery advertising start/stop.
- `ConfigurationCompatibilityWarnings.GetWarnings(...)`. Implemented for
  legacy config-key and retry-floor compatibility warnings.
- `SoulseekNetworkExceptionClassifier.IsExpected(...)`. Implemented for
  expected Soulseek network/disconnect exception classification.
- `SoulseekClientOptionsFactory.CreateInitial(...)`. Implemented for initial
  Soulseek client listener, transfer, diagnostics, and obfuscation runtime
  options.
- `AppPathResolver.ResolveAppRelativePath(...)`. Implemented for app-relative
  write-path resolution.
- `WebHtmlRewriteRules.Create(...)`. Implemented for URL-base-aware web asset
  rewrite rules.
- `AntiforgeryCookieRecovery`. Implemented for stale antiforgery token
  detection, stale cookie stripping, and retrying token issuance after key-ring
  mismatch.
- `AddSlskdConfigurationProviders(...)`. Implemented for default values,
  environment variables, YAML, command-line values, and volatile overlay
  configuration source composition.
- `StartupFileSystem`. Implemented for startup directory validation,
  configuration-file recreation, generated certificate export, and owned
  physical file provider construction.
- `QuicOverlayFactory`. Implemented for QUIC overlay/data client construction,
  overlay server construction, and standalone UDP overlay selection.
- `StartupLogging.Configure(...)`. Implemented for global Serilog setup and
  log-record emission into the existing Program log event/buffer surface.
- `StartupShutdownTelemetry.Install(...)`. Implemented for process-exit,
  unhandled-exception, and unobserved-task telemetry wiring.
- `StartupConsoleOutput`. Implemented for command-line argument help,
  environment-variable listing, and startup logo rendering.
- `StartupSqlite.InitOrFailFast(...)`. Implemented for SQLitePCL provider
  initialization and serialized threading validation.
- `ApplicationRuntimeInfo`. Implemented for assembly/informational version
  normalization, semantic/full version strings, canary/development flags, and
  executable-path lookup.
- `StartupSingleInstance`. Implemented for startup mutex-name construction.
- `StartupExceptionClassifier`. Implemented for unobserved-task exception
  classification.
- `StartupCommandMode`. Implemented for primitive startup command-mode handling.
- `StartupApplicationDirectoryResolver`. Implemented for startup
  application-directory default resolution and default directory validation.
- `StartupConfiguration`. Implemented for startup configuration provider
  loading, binding, diagnostics, and validation.
- `StartupDiagnostics`. Implemented for configured startup identity, system,
  directory, compatibility-warning, and logging-target diagnostics.
- `StartupWebApplicationRunner`. Implemented for ASP.NET hardening validation,
  builder configuration, service registration, DI build, pipeline setup,
  no-start handling, and run lifecycle.
- `AddSlskdTransfers(...)`. Implemented for rate limiting, downloads, uploads,
  transfer coordination, file access, and auto-replace services.
- `AddSlskdSecurity(...)`. The existing
  `Common.Security.SecurityStartup.AddSlskdnSecurity(...)` owns the
  configuration-driven application security graph and middleware; its option
  binding and HTTP middleware have unit/integration coverage. ASP.NET
  authentication and authorization remain part of `AddSlskdWebServices(...)`.
- `AddSlskdIntegrations(...)`. Implemented for VPN, Lidarr, scripts, webhooks,
  now-playing, listening-party, and the existing Lidarr hosted workers.
- `AddSlskdTelemetry(...)`. Implemented for Prometheus, reports, and telemetry
  aggregation services.
- `AddSlskdUserData(...)`. Implemented.
- `AddExperimentalDiscovery(...)`. Implemented across the existing domain
  owners: `AddSlskdTransferDiscoveryServices(...)` owns backfill, mesh hash
  sync, source discovery and rescue; `AddSlskdCapabilitiesAndRendezvousServices(...)`
  owns DHT rendezvous and peer discovery; `AddSlskdCoreApplicationServices(...)`
  owns Soulseek discovery; and `AddSlskdIntegrationAndMediaServices(...)`
  owns the discovery graph. Their existing call order stays in
  `AddSlskdExperimentalFeatureGraph(...)`; no extra wrapper layer is needed.
- `AddExperimentalMesh(...)`. Implemented across
  `AddSlskdExperimentalMeshServices(...)` for mesh/DHT transport and realm
  services, `AddSlskdCapabilitiesAndRendezvousServices(...)` for DHT overlay
  peers, `AddSlskdTransferDiscoveryServices(...)` for hash synchronization,
  and `AddSlskdMeshStreamingServices(...)` for content/peer streams at the
  original core graph position. Existing feature gates, stream admission, and
  hosted-service order are covered by focused tests.
- `AddExperimentalSongId(...)` / `AddSlskdSongId(...)`. Implemented in
  `SongIdServiceCollectionExtensions`; focused descriptor-count coverage
  checks all three services register once.

Acceptance criteria:

- `Program.cs` no longer directly imports every experimental vertical. Complete
  for this pass; Program now owns entrypoint orchestration, public process
  state, command-line attribute binding, and the public log event/buffer bridge.
  Runtime service registration, the experimental feature graph,
  VirtualSoulfind, multi-source/transfer-discovery, MediaCore/PodCore,
  mesh/DHT, capability-rendezvous, integration/media, host diagnostics,
  post-build startup tasks, Web listener/Kestrel setup, app run/lifecycle hooks,
  configuration compatibility warnings, Soulseek option construction,
  app-relative path resolution, web HTML rewrite rules, antiforgery recovery,
  configuration provider composition, startup filesystem checks, QUIC overlay
  construction, logging/shutdown telemetry, CLI output, SQLite initialization,
  runtime identity, startup directory preparation, startup configuration
  loading, startup diagnostics, and ASP.NET build/pipeline/run flow are owned by
  focused helpers.
- Experimental features are explicitly gated.
- Startup logs show enabled experimental features.

## 8. Add feature gate enforcement

Status: runtime gate status is now exposed through the native capabilities
response. A shared frontend hook waits for that response before gated API
requests, refreshes when a page returns to the foreground and once per minute,
and treats missing gate metadata from older servers as enabled for
compatibility. Messaging and MediaCore hide disabled Pods controls and avoid
PodCore requests; Search does the same for SongID and federated
recommendations; System does the same for Mesh, DHT rendezvous,
VirtualSoulfind, and multi-source downloads. Network sync actions are disabled
when Mesh is off, and the parent MediaCore workflow index is hidden with its
gated PodCore panel so its anchor links cannot point at missing content. The
response uses the same effective gate decisions as the controllers, including
Mesh overlay and DHT service settings. The filter returns 404 for
configuration-disabled gates and 410 with the canonical slskr link for
MovedToSlskr. The only inventory row marked moved is a documentation handoff
with no active runtime route. A unit test pins FeatureId to the seven shipped
gated surfaces so design-only inventory entries stay out of runtime
registration.

Minimum implementation:

- `FeatureId` enum. Done.
- `FeatureGate` service. Done.
- Controller/action attribute or explicit helper for experimental API
  endpoints. Done for the inventoried surfaces.
- UI route metadata or status endpoint so the frontend can hide disabled features. Done for all current UI consumers of the seven runtime gates through featureGates and the shared gate hook.

Acceptance criteria:

- Disabled experimental API surfaces return explicit disabled/404 behavior.
  The inventoried SongID, mesh, DHT, pods, social federation, VirtualSoulfind,
  and multi-source APIs are gated and the disabled response is covered.
- A `MovedToSlskr` gate returns 410 with an error message and project link.
  No current runtime route is classified as moved.
- Design-only features are not runtime `FeatureId`s; the enum inventory test
  will fail if one is added without an explicit runtime decision.
- A disabled UI surface does not poll its gated API, expose callable controls,
  or leave links to content hidden with the gate.

## 8a. Dependency ownership inventory

Status: first pass complete. `docs/dependencies.md` now classifies active runtime call sites for TagLibSharp, AWSSDK.S3, Zeroconf, Dapper, System.Reactive, MonoTorrent, NSec, MessagePack, telemetry, and build-only tooling.

Remaining follow-up:

- Revisit `dotNetRDF` only if Solid/WebID moves out of this app.
- Revisit `MathNet.Numerics` only if MediaCore hashing changes implementation.
- Decide whether the remaining Microsoft.CodeAnalysis helpers belong in runtime or a tooling project.
- Decide whether telemetry/metrics and LAN discovery need explicit feature gates beyond existing options.

## 9. Move custom MSBuild tasks out of the app assembly

Status: build task relocation complete. Analyzer suppression audit documented.
`CodeAnalysisBuildTask`, `TestCoverageBuildTask`, and `RegressionBuildTask` now
compile from linked CodeQuality sources in `tools/slskd.BuildTasks`, while the
runtime app excludes those task classes and no longer references
`Microsoft.Build.*` packages directly.

Acceptance criteria:

- `src/slskd/slskd.csproj` no longer loads MSBuild tasks from `slskd.dll`. Done.
- Build tasks live in a separate project or are removed. Done.
- Runtime package dependencies for MSBuild are removed. Done.
- Runtime Roslyn dependencies remain because `BuildTimeAnalyzer` and `SlskdnAnalyzer` still compile in the app; split them later if those helpers leave runtime.
- `docs/analyzer-suppressions.md` stays in sync with project-wide `NoWarn` entries.

## 10. Add DownloadService regression tests

Status: complete (2026-10-03). Focused coverage protects in-progress
duplicates, supersedes completed transfers, records terminal failures when the
background start path throws, serializes same-user enqueues, and allows
different users to enqueue concurrently. Cancellation-source cleanup is
verified after cancellation, failure, and successful completion by confirming
terminal transfers cannot be cancelled again. The shutdown regression verifies
active cancellation and waits for the in-flight task to drain. The
`AsNoTracking()` query remains explicit, with a 10,000-record history regression
confirming only the requested filename is materialized.

Required cases:

- Duplicate enqueue rejected.
- Existing in-progress transfer protected. Done.
- Completed old transfer can be superseded. Done.
- Enqueue exception moves transfer to terminal failed state. Done.
- CTS cleanup after cancel/fail/complete. Done.
- Shutdown cancels active transfers and drains their tasks. Done.
- Per-user semaphore serializes same-user enqueue. Done.
- Different users can enqueue concurrently. Done.
- `AsNoTracking()` behavior remains intentional. Done.

## 11. Add CI test job after branch builds locally

Status: covered by the existing CI release-gate step (2026-10-03). The
`ci.yml` build job invokes `packaging/scripts/run-release-gate.sh` for pull
requests, version tags, and manual dispatch; that gate already runs the scoped
`tests/slskd.Tests.Unit` project. The full local `dotnet test` run passed, so no
duplicate workflow job or trigger change is needed.

Do not add an always-on full test job until the branch is known to build locally on the intended .NET SDK. The repo currently targets `net10.0`, so CI image/toolchain availability should be validated first.

Suggested manual command:

```bash
dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore
```

The standalone unit-test command remains useful for local iteration. The
release-gate invocation is the CI owner of that test project.
