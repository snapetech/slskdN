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
  modules, and E2E host diagnostics are also owned by a bootstrap extension.
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
