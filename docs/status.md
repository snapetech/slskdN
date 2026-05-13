# slskdN Implementation Status

This page summarizes what slskdN should claim publicly. `FEATURE_INVENTORY.md` is the canonical table; this document is the user/contributor-facing summary.

## What works today

These areas appear to be concrete shipped implementation areas, but still need targeted tests and smoke notes before stronger release claims are made:

- Core slskd-compatible daemon behavior.
- Normal single-source Soulseek download flow.
- Basic Web UI and API surfaces inherited from slskd and extended by slskdN.
- Path validation utilities (`PathGuard`).
- Content type / magic-byte verification utilities (`ContentSafety`).

## Experimental in slskdN

These areas may have code, UI, config, or partial wiring, but must remain opt-in and clearly labelled until tests and live smoke notes exist:

- Auto-replace / transfer rescue.
- Wishlist and background search.
- Advanced search filters and smart ranking.
- Now Playing / scrobble integrations.
- Integrated player and local streaming.
- Listening parties.
- Prometheus metrics dashboard.
- Soulseek native discovery.
- Type-1 obfuscation.
- MusicBrainz, AcoustID, Chromaprint, auto-tagging, and library health.
- Lidarr integration.
- VPN agent / port-forward integration.
- DHT rendezvous, mesh overlay, hash gossip, and peer capability exchange.
- Pod system and Gold Star Club behavior.
- VirtualSoulfind and social federation.
- Multi-source / accelerated download paths.

## Design documents / roadmap only

These must not be marketed as implemented unless real runtime call sites, tests, and smoke notes are added:

- NetworkGuard as a central incoming-message guard.
- PeerReputation as a security reputation system.
- CryptographicCommitment.
- ProofOfStorage.
- ByzantineConsensus.
- Honeypots.
- Canary traps.
- Entropy monitoring.
- Paranoid mode.
- Advanced adversarial-resilience protocols beyond concrete implemented checks.

## Moved or likely moving to slskr

The README currently points testers toward `snapetech/slskr` as the forward-looking Rust daemon/API/Web UI stack. Any feature whose active implementation target is slskr should be clearly marked as moved instead of presented as slskdN-stable.

Candidates requiring classification:

- Forward-looking mesh/runtime work.
- Future daemon/API/Web UI parity work.
- Any feature where the slskdN implementation has become a prototype or compatibility bridge.

## Known gaps

- README feature claims are broader than the verified stable implementation set.
- `Program.cs` still owns process/configuration bootstrapping, but runtime service composition lives in `Bootstrap/RuntimeServiceCollectionExtensions.cs`, ASP.NET service registration lives in `Bootstrap/WebServiceCollectionExtensions.cs`, the request pipeline lives in `Bootstrap/WebApplicationPipelineExtensions.cs`, application-host wiring lives in `Bootstrap/ApplicationHostServiceCollectionExtensions.cs`, core app services live in `Bootstrap/CoreApplicationServiceCollectionExtensions.cs`, integration/media registration lives in `Bootstrap/IntegrationAndMediaServiceCollectionExtensions.cs`, multi-source feature registration lives in `Bootstrap/MultiSourceFeatureServiceCollectionExtensions.cs`, the remaining experimental feature graph lives in `Bootstrap/ExperimentalFeatureGraphServiceCollectionExtensions.cs`, and user-data persistence registration lives in `Bootstrap/UserDataServiceCollectionExtensions.cs`; continue splitting the broad experimental module into smaller bounded registrations.
- Some security documentation is written as implementation plans and pseudo-code, not shipped behavior.
- Startup hardening now uses bind exposure analysis instead of treating “port enabled” as equivalent to “non-loopback bind”; broader startup matrix tests still need to be added around full host construction.
- `HashFromAudioFileEnabled` is known-unavailable by startup validation and has no public command-line or environment toggle; any future re-exposure needs a real PCM extraction capability check.
- Experimental feature gates now exist for high-risk surfaces. SongID, mesh, DHT, pods, social federation, VirtualSoulfind, and multi-source APIs are gated and remain enabled by default through their `feature.*` switches.
- SongID now exposes `/api/v0/songid/capabilities` so optional provider lanes are reported from runtime config/tool availability instead of implied by docs.
- Dependency ownership has an initial call-site-backed inventory in `docs/dependencies.md`; `dotNetRDF` and `MathNet.Numerics` remain active for Solid/WebID and MediaCore hashing, while Roslyn CodeQuality helpers still need a runtime-vs-tooling decision.
- Custom build quality tasks now live in `tools/slskd.BuildTasks`; remaining build-quality cleanup should target broad suppressions and runtime-adjacent Roslyn helpers.
- Analyzer suppressions now have a first-pass audit in `docs/analyzer-suppressions.md`; the app build is warning-clean, but broad project suppressions still need reduction or targeting over time.
- Download flow now has focused regression coverage for in-progress duplicate protection, completed-transfer supersession, terminal failed cleanup, and per-user enqueue semaphore behavior; CTS cleanup remains the main targeted follow-up before deeper rescue/swarm/ranking refactors.

## Release rule

A feature is not stable merely because it appears in README, config, a controller, a service registration, or a screenshot. Stable requires:

1. Concrete implementation.
2. Feature inventory row.
3. Tests.
4. Manual or automated smoke path.
5. Accurate documentation.
6. No startup validator or capability reporter saying the feature is unavailable.
