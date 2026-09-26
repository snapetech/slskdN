# Upstream slskd parity audit (2026-03-24 through 2026-09-24)

## Scope and method

Reviewed every pull request merged into `slskd/slskd:master` from 2026-03-24
00:00 UTC through 2026-09-24 23:59 UTC. The GitHub search API returned 77
merged PRs. Each title and changed-file list was checked against this fork's
current source, configuration, dependency lockfiles, release history, and
security/path-handling implementation. PRs from upstream are references only;
this fork's license rollback plan excludes upstream's post-0.25.0 source and
license terms. Functional equivalents below are implemented independently.

`Implement` rows identify an applicable gap. `Already covered` means this fork
has the same behavior or a stronger equivalent. `Keep as-is` means the upstream
change does not fit this fork's architecture, compatibility contract, or
documented behavior. `Dependency` rows were also checked against the resolved
web lockfile and current .NET project references.

## Audit and disposition

| Upstream PR | Change | Area | Decision and reason |
|---|---|---|---|
| [#1850](https://github.com/slskd/slskd/pull/1850) | Stream file and directory enumeration to reduce scan memory | Performance | **Implement.** The scanner currently materializes every directory array and every per-directory file array before processing. Keep required directory deduplication, but stream enumeration into the dedupe set and process files lazily. |
| [#1848](https://github.com/slskd/slskd/pull/1848) | Checkpoint share database after scan and backup | Data durability | **Implement.** The share repository vacuums but does not checkpoint its WAL after a scan or a backup. |
| [#1843](https://github.com/slskd/slskd/pull/1843) | Rewrite favicon URL for hosted base paths | Interop | **Already covered.** The fork injects a `<base>` tag for the configured URL base, so the relative `./favicon.ico` in `index.html` resolves under nested routes. The footer uses a bundled fork logo. |
| [#1842](https://github.com/slskd/slskd/pull/1842) | Make conversation activation insert/update atomic | Concurrency | **Already covered.** `ConversationService.ActivateConversation` serializes its read/insert/update transaction under a shared lock. |
| [#1840](https://github.com/slskd/slskd/pull/1840) | Add benchmark-test scaffolding | Developer tooling | **Already covered.** The fork has a dedicated performance test project and BenchmarkDotNet infrastructure. |
| [#1838](https://github.com/slskd/slskd/pull/1838) | Fix footer favicon relative path | Interop | **Already covered.** The fork footer references its bundled icon, not a route-relative favicon URL. |
| [#1835](https://github.com/slskd/slskd/pull/1835) | Update browserslist | Dependency | **Already covered.** The lockfile resolves browserslist 4.28.8. |
| [#1834](https://github.com/slskd/slskd/pull/1834) | Update nanoid | Dependency | **Already covered.** The lockfile resolves nanoid 3.3.19. |
| [#1833](https://github.com/slskd/slskd/pull/1833) | Update fast-uri | Dependency | **Not applicable.** `fast-uri` is absent from the fork's dependency tree. |
| [#1829](https://github.com/slskd/slskd/pull/1829) | Enable disk logging by default and reduce default retention to 30 days | Operations | **Implement.** Disk logging is currently opt-in and retains logs for 180 days. Make the default durable and bounded, preserving the existing opt-out. |
| [#1828](https://github.com/slskd/slskd/pull/1828) | Add a debug API for the internal Soulseek transfer lists | Diagnostics/security | **Keep as-is.** The fork has authenticated transfer snapshots, event streams, diagnostics, and memory-dump controls. Adding a second internal-list endpoint would expose implementation details without a demonstrated operational gap. |
| [#1827](https://github.com/slskd/slskd/pull/1827) | Cancel transfers whose remote enqueue acknowledgement times out | Reliability | **Already covered.** Timed-out and cancelled enqueue waits call `CancelTrackedDownload` before failing the transfer. |
| [#1826](https://github.com/slskd/slskd/pull/1826) | Update brace-expansion | Dependency/security | **Already covered.** The lockfile resolves brace-expansion 1.1.18 and 5.0.9. |
| [#1813](https://github.com/slskd/slskd/pull/1813) | Update js-yaml | Dependency | **Not applicable.** The fork uses the `yaml` package and does not depend on `js-yaml`. |
| [#1811](https://github.com/slskd/slskd/pull/1811) | Update fast-uri | Dependency | **Not applicable.** `fast-uri` is absent from the fork's dependency tree. |
| [#1808](https://github.com/slskd/slskd/pull/1808) | Update Pushbullet environment-variable documentation | Integrations | **Not applicable.** The fork does not ship the upstream Pushbullet integration; its current integration set is documented separately. |
| [#1802](https://github.com/slskd/slskd/pull/1802) | Update shell-quote | Dependency/security | **Not applicable.** `shell-quote` is absent from the fork's dependency tree. |
| [#1793](https://github.com/slskd/slskd/pull/1793) | Remove deprecated top-level permissions from example config | Configuration | **Keep as-is.** The fork's `permissions.file.mode` setting is active functionality, documented, and distinct from upstream's retired permission objects. |
| [#1792](https://github.com/slskd/slskd/pull/1792) | Remove table-row hover color | UI | **Keep as-is.** Presentation-only upstream styling does not address a fork defect; fork table themes and selection states are independently implemented. |
| [#1791](https://github.com/slskd/slskd/pull/1791) | Update websocket-driver | Dependency/security | **Not applicable.** `websocket-driver` is absent; the fork uses a newer `ws` dependency. |
| [#1790](https://github.com/slskd/slskd/pull/1790) | Update ws | Dependency/security | **Already covered.** The lockfile resolves ws 8.21.0, above the upstream dependency line. |
| [#1789](https://github.com/slskd/slskd/pull/1789) | Redesign Browse and virtualize its directory tree | Performance/UI | **Implement the applicable part.** Browse is already fork-redesigned and uses a worker, but its expanded tree still renders up to 2,000 DOM rows and then truncates. Use the existing `react-window` dependency to window visible rows and remove the arbitrary cap. |
| [#1788](https://github.com/slskd/slskd/pull/1788) | Recursively select and download Browse subdirectories | Feature | **Already covered.** Browse recursively collects files for a selected subtree and sends them through the batch download endpoint. |
| [#1786](https://github.com/slskd/slskd/pull/1786) | Set `Cache-Control: no-cache` on `index.html` | Operations/interop | **Implement.** Neither static `index.html` nor the SPA fallback sets a revalidation header, which can leave clients on stale assets after an update. |
| [#1781](https://github.com/slskd/slskd/pull/1781) | Update .NET dependencies and Soulseek.NET to 10.0.2 | Dependencies/protocol | **Keep as-is.** The fork targets .NET 10 and deliberately maintains a patched, pinned Soulseek runtime; current Microsoft and common library versions are newer than upstream's. Do not replace that runtime with an unreviewed package. |
| [#1780](https://github.com/slskd/slskd/pull/1780) | Fix chat initiation after a component refactor | UI bug | **Not applicable.** The fork replaced those upstream Chat components with its unified Messaging implementation. |
| [#1778](https://github.com/slskd/slskd/pull/1778) | Add a Dashboard and make it the default landing page | Feature/UI | **Keep as-is.** The fork intentionally opens on Search and has fork-specific System, Metrics, Security, and transport dashboards. Upstream's transfers-oriented landing page is not a direct fit. |
| [#1776](https://github.com/slskd/slskd/pull/1776) | Update js-yaml | Dependency | **Not applicable.** `js-yaml` is absent from the fork's dependency tree. |
| [#1771](https://github.com/slskd/slskd/pull/1771) | Add bottom padding to Chat and Rooms | UI | **Already covered.** The fork measures its persistent footer and exposes a CSS height variable used by views. |
| [#1770](https://github.com/slskd/slskd/pull/1770) | Update form-data | Dependency/security | **Already covered.** The lockfile resolves form-data 4.0.6. |
| [#1769](https://github.com/slskd/slskd/pull/1769) | Broadcast real-time transfer metrics and display them in the footer | Telemetry/UI | **Already covered.** The fork footer displays live rates and transfer/mesh statistics, with visibility-aware polling and in-flight request guards. |
| [#1760](https://github.com/slskd/slskd/pull/1760) | Use current X.509 loading APIs and remove retired permission objects | Security/maintenance | **Already covered.** Certificate generation and file loading use `X509CertificateLoader`; remaining certificate conversions are from in-memory certificate instances, not obsolete byte/file loaders. |
| [#1759](https://github.com/slskd/slskd/pull/1759) | Update dependencies and Grafana Loki log format | Dependency/operations | **Already covered.** The fork's Serilog and Loki packages are newer, and its startup logger configures a fork-specific structured formatter. |
| [#1758](https://github.com/slskd/slskd/pull/1758) | Make all `bin/` scripts OS agnostic | Developer tooling | **Keep as-is.** The fork's scripts are Bash-based and used by Linux CI/release automation; no supported Windows shell contract is defined for these commands. |
| [#1757](https://github.com/slskd/slskd/pull/1757) | Fix a missing `$` in a default destination expression | Bug fix | **Not applicable.** The fork uses its own destination resolver, per-request destinations, and completed-path templates rather than that expression. |
| [#1756](https://github.com/slskd/slskd/pull/1756) | Move permissions under destination and override umask for directory modes | Configuration/security | **Keep as-is.** The fork documents that explicit file modes remain bounded by process umask. Silently changing that security contract or repurposing `permissions.file.mode` as a directory mode is not safe; a separate opt-in mode design would be needed. |
| [#1751](https://github.com/slskd/slskd/pull/1751) | Update launch-editor | Dependency | **Not applicable.** `launch-editor` is absent from the fork's dependency tree. |
| [#1749](https://github.com/slskd/slskd/pull/1749) | Add cross-platform path and filename handling functions | Security/reliability | **Already covered.** The fork has host-independent remote-path normalization plus `PathGuard`, allowed-root checks, symlink resolution, and traversal tests. |
| [#1746](https://github.com/slskd/slskd/pull/1746) | Add granular download-location options | Feature | **Already covered.** The fork has configured destinations, per-request destination selection, completed layouts, and sanitized path templates. |
| [#1743](https://github.com/slskd/slskd/pull/1743) | Rename retry `incomplete` to `partial` and default retries to resume | Configuration | **Keep as-is.** The fork already defaults to resume and exposes a richer retry strategy under a legacy-compatible `incomplete` key; a rename would add migration risk without changing behavior. |
| [#1740](https://github.com/slskd/slskd/pull/1740) | Correct cross-platform path manipulation for drive-relative paths | Security/reliability | **Already covered.** The fork's path guard rejects rooted peer paths, normalizes Windows and Unix separators, and validates canonical paths against allowed roots. |
| [#1737](https://github.com/slskd/slskd/pull/1737) | Fix incomplete-download filename handling | Reliability | **Already covered.** The fork derives retry output from the same guarded local-filename mapping used by the download stream and has resume/retry coverage. |
| [#1735](https://github.com/slskd/slskd/pull/1735) | Strip trailing slash from Relay controller address | Interop | **Implement.** The agent currently appends `/hub/relay` and constructs `HttpClient.BaseAddress` directly from configured text, so a trailing slash can produce malformed hub URLs and relative-request resolution. |
| [#1734](https://github.com/slskd/slskd/pull/1734) | Parse share aliases when the local path contains `]` | Configuration bug | **Implement.** `SharesOptions.Digest` uses a greedy alias regex that consumes through the last `]`, corrupting paths with a closing bracket. |
| [#1733](https://github.com/slskd/slskd/pull/1733) | Add/adjust path and string validation attributes | Security | **Already covered.** The fork validates API/configuration boundaries and applies stronger canonical-root and peer-path checks through `PathGuard`. |
| [#1729](https://github.com/slskd/slskd/pull/1729) | Reject traversal paths from untrusted callers | Security | **Already covered.** Explicit destinations are normalized against configured roots; peer paths pass traversal, symlink, and root-containment checks. |
| [#1727](https://github.com/slskd/slskd/pull/1727) | Add path-handling helpers | Security/maintenance | **Already covered.** The fork has a dedicated `PathGuard` and destination resolver with focused tests. |
| [#1725](https://github.com/slskd/slskd/pull/1725) | Update Babel SystemJS transform plugin | Dependency | **Not applicable.** The fork uses Vite and has no SystemJS transform plugin. |
| [#1724](https://github.com/slskd/slskd/pull/1724) | Update fast-uri | Dependency | **Not applicable.** `fast-uri` is absent from the fork's dependency tree. |
| [#1723](https://github.com/slskd/slskd/pull/1723) | Update axios | Dependency/security | **Already covered.** The package manifest and lockfile resolve axios 1.20.0, above upstream's 1.15.2. |
| [#1720](https://github.com/slskd/slskd/pull/1720) | Add Transfer Batches | Feature/API | **Already covered.** The fork persists `BatchId`, accepts grouped downloads, exposes batch status, and preserves batch identity through retry/auto-replace. |
| [#1719](https://github.com/slskd/slskd/pull/1719) | Fix some users' Chat/Room input handling | UI bug | **Not applicable.** This fork's unified Messaging/Rooms views do not use the old DOM-ref and timer-based input path changed upstream. |
| [#1716](https://github.com/slskd/slskd/pull/1716) | Update follow-redirects | Dependency/security | **Already covered.** The lockfile resolves follow-redirects 1.16.0. |
| [#1714](https://github.com/slskd/slskd/pull/1714) | Normalize IPv4-mapped IPv6 before Relay CIDR checks | Security/interop | **Already covered.** `RelayHub.RemoteIpAddress` normalizes mapped IPv4 before both agent CIDR authorization and peer-address recording. |
| [#1713](https://github.com/slskd/slskd/pull/1713) | Allow username-pattern blacklisting | Security/feature | **Already covered.** User matching supports configured username patterns in addition to exact and CIDR rules. |
| [#1711](https://github.com/slskd/slskd/pull/1711) | Fix runtime configuration diffing | Reliability | **Already covered.** The fork's configuration diff path handles missing/null legacy sections and its compatibility layer maps prior layouts. |
| [#1709](https://github.com/slskd/slskd/pull/1709) | Remove Docker VOLUME and create app directory in entrypoint | Operations | **Already covered.** The fork's image uses its custom entrypoint/app-directory ownership flow and its packaging validation rejects the old `/app` volume behavior. |
| [#1708](https://github.com/slskd/slskd/pull/1708) | Preserve legacy root Docker behavior when PUID/PGID are absent | Operations | **Already covered.** The fork entrypoint preserves root execution unless an explicit UID/GID or user is selected. |
| [#1705](https://github.com/slskd/slskd/pull/1705) | Update Docker documentation | Documentation | **Already covered.** Fork Docker and download-permission guides document this fork's container and ownership behavior. |
| [#1704](https://github.com/slskd/slskd/pull/1704) | Update configuration documentation | Documentation | **Already covered.** The fork maintains a separate full config reference matching its extended options. |
| [#1698](https://github.com/slskd/slskd/pull/1698) | Fail on deprecated configuration keys | Configuration | **Already covered.** The fork warns for legacy layouts and deliberately continues accepting them through compatibility mapping. Failing those keys would break the documented migration contract. |
| [#1699](https://github.com/slskd/slskd/pull/1699) | Add regex username matching to blacklist group | Security/feature | **Already covered.** `Groups.Blacklisted.Patterns` is validated and applied through `UserService.UsernameMatcher`, with configuration and docs already present. |
| [#1696](https://github.com/slskd/slskd/pull/1696) | Fix root favicon path for nested hosting | Interop | **Already covered.** The fork's HTML rewrite inserts a base tag for nested deployments, and `index.html` uses a relative favicon path. |
| [#1695](https://github.com/slskd/slskd/pull/1695) | Support Docker PUID/PGID | Operations | **Already covered.** The fork image, entrypoint, and Unraid template implement PUID/PGID with explicit-user handling. |
| [#1694](https://github.com/slskd/slskd/pull/1694) | Update axios | Dependency/security | **Already covered.** The fork resolves axios 1.20.0. |
| [#1693](https://github.com/slskd/slskd/pull/1693) | Upgrade to .NET 10 | Platform | **Already covered.** All app and test projects target .NET 10. |
| [#1692](https://github.com/slskd/slskd/pull/1692) | Update lodash | Dependency/security | **Already covered.** The lockfile resolves lodash 4.18.1. |
| [#1691](https://github.com/slskd/slskd/pull/1691) | Update Soulseek.NET to 10.0.0 | Protocol/dependency | **Keep as-is.** The fork uses its pinned, modified runtime source; an upstream package swap would discard fork protocol patches and is not a safe parity update. |
| [#1690](https://github.com/slskd/slskd/pull/1690) | Update NOTICE and add a license line to the startup banner | Licensing | **Keep as-is.** The fork has a separate license rollback/NOTICE contract. Do not copy post-0.25.0 licensing changes or upstream attribution text. |
| [#1689](https://github.com/slskd/slskd/pull/1689) | Update lodash-es | Dependency/security | **Already covered.** The fork pins lodash-es at 4.18.1. |
| [#1687](https://github.com/slskd/slskd/pull/1687) | Interlock shared scanner counters | Concurrency | **Already covered.** Scanner progress counters use `Interlocked` updates and snapshots. |
| [#1684](https://github.com/slskd/slskd/pull/1684) | Update picomatch | Dependency/security | **Already covered.** The lockfile resolves picomatch 4.0.7, newer than the upstream 2.x target. |
| [#1682](https://github.com/slskd/slskd/pull/1682) | Fix runtime configuration update errors | Reliability | **Already covered.** Current configuration reload and null-safe diff behavior include the upstream correction. |
| [#1680](https://github.com/slskd/slskd/pull/1680) | Update yaml | Dependency/security | **Already covered.** The fork uses `yaml` 2.9.1. |
| [#1676](https://github.com/slskd/slskd/pull/1676) | Update flatted | Dependency/security | **Already covered.** The lockfile resolves flatted 3.4.4. |
| [#1675](https://github.com/slskd/slskd/pull/1675) | Append Additional Terms to AGPLv3 | Licensing | **Do not adopt.** The fork's recorded license decision explicitly excludes upstream's post-0.25.0 Additional Terms and source tree. |
| [#1664](https://github.com/slskd/slskd/pull/1664) | Add transfer retries and batches | Feature/reliability | **Already covered.** The fork has bounded retry/backoff, resumable partial downloads, persistent batches, and destination-aware retry handling. |

## Applicable changes implemented from this audit

1. Share scanning now streams directory and file enumeration while preserving
   exclusions, reparse-point protections, cancellation, and progress counters.
2. The share database WAL is checkpointed after scan maintenance and backups.
3. Disk logging is enabled by default with 30-day retention and the existing
   opt-out preserved.
4. Successful HTML responses for the static index and SPA fallback receive a
   `Cache-Control: no-cache` header before response headers are sent.
5. Browse now windows visible directory rows, removing the fixed 2,000-row
   ceiling while keeping the DOM bounded.
6. Share aliases now stop at the first closing bracket, preserving `]` inside
   local paths.
7. Relay controller URLs now have exactly one trailing slash, preserving any
   configured base path for both hub and HTTP clients.

The mapped-IP finding was verified as already handled in the Relay hub.

## Validation and release tracking

After PR #338 merged to `main` on 2026-09-24, the fork had zero open issues,
zero CodeQL alerts, zero Dependabot alerts, and zero secret-scanning alerts.
Hosted PR checks passed for build/test, CodeQL, dependency and container scans,
E2E, Nix smoke, load and performance checks, release-note validation, and local
identity checks. Windows Smoke remained queued because no matching Windows
runner was available. At that audit checkpoint, PRs #326 and #327 were open
because React 19 was not compatible with the current Semantic UI React
dependency. Both PRs closed unmerged on 2026-09-25; the fork stays on React 18
until a fresh migration passes validation. The fork's license boundary,
dependency freshness, and conservative network policy remain release gates.
The stable release-note preview is run against the validated release base and
head before the tag is created.
