# Implemented Security Controls

This document is intentionally limited to security controls that have concrete implementation in this repository. Roadmap items and pseudo-code belong in `docs/security/security-roadmap.md`.

## PathGuard

`PathGuard` is an implemented filesystem path validation utility.

Current responsibilities:

- Reject null, empty, overlong, rooted, drive-letter, control-character, and traversal paths.
- Normalize Unicode before validation.
- Decode URL-encoded traversal attempts, including repeated encoding.
- Normalize path separators.
- Ensure paths remain contained within an allowed root.
- Sanitize filenames.
- Detect dangerous and safe-audio extensions.

Required follow-up:

- Add focused unit tests for traversal, double-encoding, Windows drive letters, absolute paths, root containment, Unicode normalization, and filename sanitization.
- Audit all peer/server-supplied path call sites and record them in `FEATURE_INVENTORY.md`.
- Confirm delete-file, streaming, downloads, browse, relay, and share paths use containment checks consistently.

## ContentSafety

`ContentSafety` is an implemented content-header verification utility.

Current responsibilities:

- Check common audio, image, archive, and PDF magic-byte signatures.
- Detect executable/script-like content independent of claimed extension.
- Warn on mismatched known extensions.
- Fail on dangerous executable content masquerading as non-executable content.

Required follow-up:

- Add tests for valid audio headers, short headers, unknown extensions, mismatched headers, and executable masquerading cases.
- Confirm post-download call sites actually invoke verification when configured.
- Decide whether warning results block, quarantine, log only, or surface to the UI.

## BindExposureAnalyzer

`BindExposureAnalyzer` is an implemented bind-posture classifier used to make startup hardening checks reason about actual listener exposure rather than treating "port enabled" as equivalent to "remote reachable".

Current responsibilities:

- Classify no listener, Unix-socket-only, loopback-only, wildcard, private/link-local non-loopback, public non-loopback, and unknown TCP bind addresses.
- Treat wildcard binds such as `*`, `0.0.0.0`, and `::` as remote reachable.
- Treat invalid or unclassified enabled TCP bind addresses as remote reachable so hardening checks fail closed.
- Provide `IsRemoteReachable()` for downstream startup validation.

Implemented tests:

- Loopback IPv4, loopback IPv6, and `localhost`.
- Unix socket with no TCP listener.
- Wildcard IPv4/IPv6/all-address binds.
- RFC1918, link-local IPv4, unique-local IPv6, and link-local IPv6.
- Public IPv4 and IPv6.
- Invalid bind address fallback.
- Remote-reachability classification for every exposure enum value.

Required follow-up:

- Wire `Program.cs` to pass `BindExposureAnalyzer.IsRemoteReachable(...)` into `HardeningValidator.Validate(...)` instead of deriving exposure from whether ports are enabled.
- Add startup-level HardeningValidator tests after the Program.cs wiring patch lands.

## HardeningValidator

`HardeningValidator` performs startup validation for dangerous configurations.

Current responsibilities:

- Warn or fail when authentication is disabled and remote exposure is unsafe.
- Warn or fail when remote no-auth access is enabled without CIDR restrictions.
- Warn or fail on CORS credentials with wildcard origins.
- Warn or fail when memory dumps are enabled while authentication is disabled.
- Warn or fail when metrics auth has an empty password.
- Warn or fail when `HashFromAudioFileEnabled` is enabled despite unavailable PCM extraction support.

Required follow-up:

- Wire bind exposure semantics through `BindExposureAnalyzer`; the validator must receive actual exposure information, not a boolean derived from whether ports are enabled.
- Decide whether `HashFromAudioFileEnabled` should be removed, renamed as experimental, or made conditional on a real PCM extraction capability check.

## Authentication, metrics, and diagnostics posture

The current implemented posture should be documented only where there is actual startup validation or middleware enforcement. Do not claim complete zero-trust coverage unless all relevant API, static file, streaming, SignalR, diagnostics, metrics, and integration surfaces have tests.

## Documentation rule

A security feature may only appear in this document when it has:

1. A concrete class/service/middleware implementation.
2. Known call sites or startup wiring.
3. Tests or an explicit `needs-test` entry in `FEATURE_INVENTORY.md`.
4. No contradictory startup warning saying it is unavailable.
