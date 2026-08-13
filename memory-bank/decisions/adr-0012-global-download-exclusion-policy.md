# ADR-0012: Global Download Exclusion Policy

> **Status**: Accepted
> **Date**: 2026-08-13
> **Author**: slskdN Team

## Context

The existing `filters.search.request` option filters inbound search requests,
and Wishlist/Search filters affect individual searches or browser presentation.
Neither is an authoritative policy for outbound transfers. Several outbound
paths also bypass the normal download enqueue service: pod downloads,
multi-source transfers, verification probes, backfill probes, the
VirtualSoulfind bridge, peer-preview streams, and filename-bearing
VirtualSoulfind v2 resolver backends.

Users need a daemon-wide way to avoid downloading known unwanted filename/path
categories such as acapella or instrumental releases. The policy must remain
effective when a transfer is retried, replaced, resumed, or routed through an
alternate transfer implementation.

## Decision

Add `filters.download.exclude` as a validated list of literal terms. Each term
is trimmed and matched case-insensitively as a substring of the complete remote
filename/path. Path separators are normalized for matching. The list is capped
at 100 entries, and each entry is capped at 256 characters.

Use one shared `DownloadFilter` matcher across all outbound paths. Enforce the
policy before normal enqueue database records and remote enqueue work are
created, again immediately before a queued normal transfer starts, and at the
boundaries of direct pod, multi-source, verification, backfill, swarm, bridge,
and peer-preview transfers. The v2 resolver applies the policy to HTTP,
WebDAV, S3, and LAN backend references, while opaque torrent and mesh content
identifiers are not treated as filenames. Live YAML changes cancel active
normal transfers that become blocked and prevent blocked files from resume,
retry, and auto-replace planning.

Expose blocked normal API requests as structured `download_blocked` responses
when the whole request is rejected, and include blocked items in mixed-batch
responses. The System > Policies screen edits the same YAML path and explains
that matching is literal, path-wide, case-insensitive, and applied before peer
contact.

## Consequences

### Positive

- Users get one policy with predictable semantics instead of several unrelated
  search/display filters.
- Blocked files do not create normal transfer records or contact Soulseek
  peers when rejected at enqueue time.
- Runtime configuration changes are effective without a restart and do not
  allow queued/racing transfers to bypass the policy.
- Alternate and experimental download paths share the same safety behavior.

### Negative

- Literal substring matching can be broader than a single exact filename and
  may block a legitimate path containing a configured term.
- The policy does not inspect tags or audio content, so it cannot identify a
  file whose path does not contain a configured term.
- A blocked file remains visible in historical failed/rejected transfer data
  when it was already queued before the policy changed.

### Neutral

- Existing per-search filters remain available for search-specific behavior;
  they are not treated as global download policy.
- Removing an exclusion does not automatically retry previously blocked files;
  users or automation must request them again.

## Alternatives Considered

- **Reuse Wishlist/Search filter syntax**: rejected because it is scoped to a
  saved search or client-side result display and cannot guard direct transfer
  paths.
- **Use regular expressions**: rejected for the global safety control because
  literal terms are easier to understand, safer to configure, and sufficient
  for the requested filename categories.
- **Filter only in the Web UI**: rejected because API clients, automation,
  retries, resumes, and experimental transfer paths would bypass it.

## References

- `docs/config.md` — Download Exclusions
- `config/slskd.example.yml` — example configuration
- `src/slskd/Transfers/Downloads/DownloadFilter.cs` — shared matcher
