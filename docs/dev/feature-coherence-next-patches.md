# Feature Coherence Implementation Backlog

This branch establishes the truth table, maturity-first README draft, security documentation split, coherence CI scripts, and first concrete security utility tests. The remaining work below should be implemented as small reviewable patches.

## 1. Replace README.md with README.maturity.md

Status: blocked in connector-based editing because the current README is large and full-file replacement risks truncation.

Recommended patch from a real checkout:

```bash
cp README.maturity.md README.md
bash scripts/audit-feature-coherence.sh
bash scripts/audit-readme-maturity-draft.sh
git diff -- README.md README.maturity.md
git commit -am "docs: replace README with maturity-first version"
```

Acceptance criteria:

- `README.md` points to `FEATURE_INVENTORY.md` and `docs/status.md`.
- The README no longer markets roadmap-only security systems as implemented.
- The README clearly distinguishes stable, experimental, roadmap-only, and moved-to-slskr work.

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

Status: `PathGuard` has focused unit tests. Call-site audit still needed.

Search targets:

```bash
rg "Path\.Combine|Path\.GetFullPath|File\.Delete|FileStream|OpenRead|OpenWrite|Move\(|Copy\(|Delete\(" src/slskd src/slskdN.VpnAgent tests
rg "NormalizeAndValidate|NormalizeAbsolutePathWithinRoots|PathGuard" src tests
```

Acceptance criteria:

- Peer/server-supplied paths go through `PathGuard` or have a documented reason not to.
- Delete-file, streaming, downloads, browse, relay, and share paths are explicitly covered.
- Any bypass gets a TODO tied to an issue or a test.

## 5. Audit ContentSafety call sites and policy

Status: `ContentSafety` has focused unit tests. Runtime policy still needs confirmation.

Acceptance criteria:

- Post-download verification call site is identified.
- Policy is explicit for mismatch warnings: block, quarantine, log only, or surface in UI.
- Dangerous executable masquerading as media fails closed when content safety is enabled.
- Integration tests cover the post-download path once policy is decided.

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
The remaining antiforgery Program wrappers were removed after the MVC CSRF
filter and focused tests moved to `AntiforgeryCookieRecovery` directly.

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
- `AddSlskdTransfers(...)`
- `AddSlskdSecurity(...)`
- `AddSlskdIntegrations(...)`
- `AddSlskdTelemetry(...)`
- `AddSlskdUserData(...)`. Implemented.
- `AddExperimentalDiscovery(...)`. Partly covered by `AddSlskdExperimentalFeatureGraph(...)`.
- `AddExperimentalMesh(...)`. Partly covered by `AddSlskdExperimentalFeatureGraph(...)`.
- `AddExperimentalSongId(...)` / `AddSlskdSongId(...)`. Started.

Acceptance criteria:

- `Program.cs` no longer directly imports every experimental vertical. In progress;
  the broad graph moved and VirtualSoulfind/multi-source/transfer-discovery/
  MediaCore/PodCore/mesh/capability-rendezvous/integration-media slices are now
  separate. The remaining coordinator module only delegates to named bootstrap
  modules, and E2E host diagnostics plus post-build startup tasks are also
  owned by bootstrap extensions. Web listener/Kestrel setup is also owned by a
  bootstrap extension, and app run/lifecycle hooks are now owned by a bootstrap
  extension. Configuration compatibility warning parsing is now owned by a
  focused configuration helper, and expected Soulseek network exception
  classification plus initial Soulseek client option construction are now owned
  by focused helpers. App-relative path resolution and web HTML rewrite rules
  are also now owned by focused helpers. Antiforgery stale-cookie recovery is
  now owned by a focused security helper, and configuration provider composition
  is now owned by a focused configuration extension. Startup filesystem checks,
  config recreation, and certificate export are now owned by a focused
  bootstrap helper. QUIC overlay construction and standalone UDP overlay
  selection are now owned by a focused mesh helper. Global logging setup and
  shutdown telemetry wiring are now owned by focused bootstrap helpers. CLI
  output and logo rendering are now owned by a focused bootstrap helper. SQLite
  provider initialization and threading validation are now owned by a focused
  bootstrap helper. Runtime version and executable-path calculation are now
  owned by a focused bootstrap helper.
- Experimental features are explicitly gated.
- Startup logs show enabled experimental features.

## 8. Add feature gate enforcement

Status: foundation implemented. `FeatureGate` now evaluates experimental feature IDs from existing options, and SongID, mesh, DHT, pods, social federation, VirtualSoulfind, and multi-source APIs are gated surfaces.

Minimum implementation:

- `FeatureId` enum. Done.
- `FeatureGate` service. Done.
- Controller/action attribute or explicit helper for experimental API endpoints.
- UI route metadata or status endpoint so the frontend can hide disabled features.

Acceptance criteria:

- Disabled experimental API surfaces return explicit disabled/404/410 behavior. Started with SongID, mesh, DHT, pods, social federation, VirtualSoulfind, and multi-source APIs.
- Moved-to-slskr features can return 410 with a message/link.
- Design-only features are not registered at runtime.

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

Status: expanded. Added focused coverage for in-progress duplicate protection,
completed-transfer supersession, terminal failed cleanup when the background
download start path throws, same-user enqueue serialization, and different-user
enqueue concurrency.

Required cases:

- Duplicate enqueue rejected.
- Existing in-progress transfer protected. Done.
- Completed old transfer can be superseded. Done.
- Enqueue exception moves transfer to terminal failed state. Done.
- CTS cleanup after cancel/fail/complete.
- Shutdown cancels active transfers.
- Per-user semaphore serializes same-user enqueue. Done.
- Different users can enqueue concurrently. Done.
- `AsNoTracking()` behavior remains intentional.

## 11. Add CI test job after branch builds locally

Status: pending local validation.

Do not add an always-on full test job until the branch is known to build locally on the intended .NET SDK. The repo currently targets `net10.0`, so CI image/toolchain availability should be validated first.

Suggested manual command:

```bash
dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore
```

If that passes locally, add a scoped workflow job for the unit test project.
