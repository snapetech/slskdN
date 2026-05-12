<h1 align="center">slskdN(OT)</h1>
<p align="center"><strong>The feature-forward Soulseek web client</strong></p>
<p align="center">
  <a href="https://github.com/snapetech/slskdn"><strong>slskdN</strong></a>
  is an unofficial fork of
  <a href="https://github.com/slskd/slskd"><strong>slskd</strong></a>.
</p>

> **Feature maturity notice**
>
> slskdN contains stable slskd-compatible behavior plus a large set of experimental and design-stage extensions. Before relying on an advanced feature, check:
>
> - [`FEATURE_INVENTORY.md`](FEATURE_INVENTORY.md) — canonical feature maturity table
> - [`docs/status.md`](docs/status.md) — user/contributor-facing implementation status
> - [`docs/security/implemented-security.md`](docs/security/implemented-security.md) — implemented security controls only
> - [`docs/security/security-roadmap.md`](docs/security/security-roadmap.md) — planned security work
> - [`docs/security/security-non-goals.md`](docs/security/security-non-goals.md) — explicit non-goals and overclaim guardrails

---

## What is slskdN?

**slskdN(OT)** is a feature-forward fork of `slskd`. The stable baseline remains normal slskd-compatible Soulseek daemon behavior, Web UI, REST API, and standard single-source transfers.

The project also contains experimental work around richer search, ranking, metadata, automation, streaming, mesh/discovery, and integrations. Those areas are intentionally tracked by maturity so documentation does not imply that every advertised idea is stable or production-ready.

## slskr / Rust rewrite status

[`snapetech/slskr`](https://github.com/snapetech/slskr) is the forward-looking Rust daemon/API/Web UI stack. Features whose active implementation target has moved to slskr should be marked `moved-to-slskr` in `FEATURE_INVENTORY.md` rather than described as stable slskdN behavior.

## Current stable baseline

The project should preserve these first:

- Core slskd-compatible daemon behavior.
- Normal single-source Soulseek transfers.
- Existing Web UI and API compatibility.
- Existing configuration compatibility where practical.

## Implemented but needs audit / tests before stronger claims

These areas have concrete implementation signals, but should remain conservative in user-facing claims until the inventory records tests and smoke paths:

- Auto-replace / conservative transfer rescue.
- Wishlist and background search.
- Advanced search filters and smart ranking.
- User notes, ratings, badges, and source history.
- Delete-file and file-type restriction workflows.
- Ntfy/Pushover/Now Playing integrations.
- Integrated player and local streaming.
- Prometheus metrics UI.
- Native Soulseek discovery.
- MusicBrainz, AcoustID, Chromaprint, auto-tagging, and library health.
- Lidarr integration.
- VPN binding / port-forward agent.
- Type-1 Soulseek peer/distributed/file-transfer obfuscation.

## Experimental / opt-in distributed systems

These must be disabled by default or explicitly gated until proven safe and documented:

- DHT rendezvous.
- Mesh overlay.
- Hash database gossip.
- Runtime capability handshakes.
- Soulseek mesh rendezvous publication.
- Pods and Gold Star Club behavior.
- VirtualSoulfind.
- Social federation.
- Service Fabric.
- Multi-source / accelerated downloads beyond conservative single-source rescue.

## Roadmap-only security claims

Do **not** describe these as implemented security guarantees unless concrete code, enforcement points, tests, and smoke paths exist:

- NetworkGuard as a central incoming-message guard.
- PeerReputation as a behavioral security system.
- CryptographicCommitment.
- ProofOfStorage.
- ByzantineConsensus.
- Honeypots.
- Canary traps.
- Entropy monitoring.
- Paranoid mode.

Implemented security controls currently documented separately:

- `PathGuard` filesystem/path containment utilities.
- `ContentSafety` magic-byte / executable masquerading checks.
- `HardeningValidator` startup checks, pending bind-exposure correction.

## Known broken or unavailable surfaces

`HashFromAudioFileEnabled` is known-unavailable unless real PCM extraction support is present. It must not be marketed as working SongID/audio-fingerprint behavior until the runtime capability exists and is tested.

## Quick start

Use the existing slskd-compatible setup paths while this maturity cleanup is underway. The current packaging and configuration docs remain the authoritative install references:

- `docs/config.md`
- `config/slskd.example.yml`
- packaging docs under `packaging/`
- release artifacts under GitHub Releases

## Documentation index

| Document | Purpose |
|---|---|
| [`FEATURE_INVENTORY.md`](FEATURE_INVENTORY.md) | Canonical feature maturity table. |
| [`docs/status.md`](docs/status.md) | Human-readable feature status summary. |
| [`docs/security/implemented-security.md`](docs/security/implemented-security.md) | Implemented security controls only. |
| [`docs/security/security-roadmap.md`](docs/security/security-roadmap.md) | Planned security systems and promotion requirements. |
| [`docs/security/security-non-goals.md`](docs/security/security-non-goals.md) | Explicit non-goals and overclaim guardrails. |
| [`docs/dependencies.md`](docs/dependencies.md) | Dependency ownership / feature mapping. |
| [`docs/analyzer-suppressions.md`](docs/analyzer-suppressions.md) | Analyzer suppression audit. |

## Release rule

A feature is not stable merely because it appears in README, screenshots, config, a controller, a service registration, or a design doc.

Stable requires:

1. Concrete implementation.
2. Feature inventory row.
3. Tests.
4. Manual or automated smoke path.
5. Accurate documentation.
6. No startup validator or runtime capability reporter saying the feature is unavailable.

---

## License

GNU Affero General Public License v3.0, same as upstream slskd.

## Acknowledgments

slskdN is built on the work of:

- upstream `slskd` by jpdillingham and contributors
- `Soulseek.NET`
- the Soulseek community
- MusicBrainz, Cover Art Archive, AcoustID, and related metadata projects
