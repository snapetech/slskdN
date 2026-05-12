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

Status: `BindExposureAnalyzer` and unit tests exist. Startup still needs wiring.

Target behavior:

- Stop deriving remote exposure from whether a web port is enabled.
- Classify the actual configured web bind address/socket.
- Pass `BindExposureAnalyzer.IsRemoteReachable(exposure)` into `HardeningValidator.Validate(...)`.
- Log the computed exposure at startup for operator/debug visibility.

Acceptance criteria:

- Auth-disabled + loopback-only bind does not fail as remote exposure.
- Auth-disabled + Unix-socket-only bind does not fail as remote exposure.
- Auth-disabled + wildcard/private/public/unknown TCP bind still fails or warns according to existing `HardeningValidator` policy.
- Startup-level tests cover the matrix.

## 3. Add HardeningValidator startup matrix tests

Status: pending Program.cs wiring.

Required cases:

- Auth disabled + `127.0.0.1` + enforce => allowed.
- Auth disabled + `localhost` + enforce => allowed.
- Auth disabled + Unix socket only + enforce => allowed.
- Auth disabled + `0.0.0.0` + enforce + no `AllowRemoteNoAuth` => fail.
- Auth disabled + `192.168.x.x` + enforce + no `AllowRemoteNoAuth` => fail.
- Auth disabled + `::` + enforce + no `AllowRemoteNoAuth` => fail.
- Auth disabled + `AllowRemoteNoAuth` + no CIDRs => fail.
- Auth disabled + `AllowRemoteNoAuth` + CIDRs => allowed.

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

Status: `HardeningValidator` says this feature requires unavailable PCM extraction support.

Preferred resolution:

- Remove public docs/config exposure for `HashFromAudioFileEnabled`, or rename to `ExperimentalHashFromAudioFileEnabled`.
- Add a runtime capability check if the feature is kept.
- Ensure SongID docs/UI do not imply local audio hashing works unless capability is present.

Acceptance criteria:

- Normal users cannot enable a known-unavailable feature casually.
- README and config examples do not market unavailable local audio hashing as working.

## 7. Split Program.cs service registration

Status: pending.

Target modules:

- `AddSlskdCore(...)`
- `AddSlskdWeb(...)`
- `AddSlskdTransfers(...)`
- `AddSlskdSecurity(...)`
- `AddSlskdIntegrations(...)`
- `AddSlskdTelemetry(...)`
- `AddExperimentalDiscovery(...)`
- `AddExperimentalMesh(...)`
- `AddExperimentalSongId(...)`

Acceptance criteria:

- `Program.cs` no longer directly imports every experimental vertical.
- Experimental features are disabled by default or explicitly gated.
- Startup logs show enabled experimental features.

## 8. Add feature gate enforcement

Status: design documented, not implemented.

Minimum implementation:

- `FeatureId` enum.
- `FeatureGate` service.
- Controller/action attribute or explicit helper for experimental API endpoints.
- UI route metadata or status endpoint so the frontend can hide disabled features.

Acceptance criteria:

- Disabled experimental API surfaces return explicit disabled/404/410 behavior.
- Moved-to-slskr features can return 410 with a message/link.
- Design-only features are not registered at runtime.

## 9. Move custom MSBuild tasks out of the app assembly

Status: documented only.

Acceptance criteria:

- `src/slskd/slskd.csproj` no longer loads MSBuild tasks from `slskd.dll`.
- Build tasks live in a separate project or are removed.
- Runtime package dependencies for MSBuild/Roslyn are removed unless truly needed at runtime.

## 10. Add DownloadService regression tests

Status: pending.

Required cases:

- Duplicate enqueue rejected.
- Existing in-progress transfer protected.
- Completed old transfer can be superseded.
- Enqueue exception moves transfer to terminal failed state.
- CTS cleanup after cancel/fail/complete.
- Shutdown cancels active transfers.
- Per-user semaphore serializes same-user enqueue.
- Different users can enqueue concurrently.
- `AsNoTracking()` behavior remains intentional.

## 11. Add CI test job after branch builds locally

Status: pending local validation.

Do not add an always-on full test job until the branch is known to build locally on the intended .NET SDK. The repo currently targets `net10.0`, so CI image/toolchain availability should be validated first.

Suggested manual command:

```bash
dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore
```

If that passes locally, add a scoped workflow job for the unit test project.
