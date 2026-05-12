# Dependency Ownership Inventory

This document tracks why runtime/build dependencies exist. It is intentionally conservative: a dependency is not justified merely because a future design might use it.

## Classification values

- `required-core` - required for stable slskd-compatible behavior.
- `required-feature` - required for a concrete implemented feature.
- `experimental-feature` - required only when an experimental feature is enabled.
- `build-only` - should not be required by the runtime application.
- `unused` - remove unless a call-site audit proves otherwise.
- `unknown` - must be resolved before release claims are strengthened.

## Audit commands

Run these from the repository root:

```bash
dotnet list src/slskd/slskd.csproj package > artifacts/package-list.txt
rg "MonoTorrent|AWSSDK|dotNetRDF|Zeroconf|MathNet|Microsoft.CodeAnalysis|Microsoft.Build|FluentFTP|TagLibSharp|NSec|OpenTelemetry|Prometheus|Dapper|MessagePack|System.Reactive" src tests docs config > artifacts/dependency-callsite-scan.txt
```

## Initial inventory

| Package / family | Current suspected owner | Classification | Required follow-up |
|---|---|---|---|
| ASP.NET Core / JwtBearer / SignalR | Web API, auth, live UI updates | required-core | Verify all auth-sensitive endpoints are covered. |
| Entity Framework Core / Microsoft.Data.Sqlite | Persistent app/transfer state | required-core | Confirm migrations and DB contexts. |
| Serilog and sinks | Logging | required-core / required-feature | Loki/HTTP sinks may be optional integration dependencies. |
| prometheus-net / DotNetRuntime / SystemMetrics | Metrics endpoint and dashboard | experimental-feature | Confirm metrics auth and feature gate. |
| OpenTelemetry packages | Telemetry/exporters | experimental-feature | Gate exporters and document egress. |
| TagLibSharp | Audio metadata/library/player/SongID surfaces | experimental-feature | Confirm concrete call sites and tests. |
| FluentFTP | FTP integration | experimental-feature | Gate integration and document credentials/egress. |
| Mono.Nat | NAT/port mapping | experimental-feature | Gate under mesh/VPN/network features. |
| MonoTorrent | DHT/rendezvous experiments | experimental-feature | Gate; do not bootstrap by default. |
| NSec.Cryptography | Mesh/security crypto | experimental-feature | Confirm protocol use and tests. |
| MathNet.Numerics | Ranking/SongID/analytics math | unknown | Keep only with concrete call sites. |
| AWSSDK.S3 | Object storage/source feeds/backup experiments | unknown | Remove or gate unless call sites justify it. |
| Zeroconf | Discovery/network experiments | experimental-feature | Gate; document network behavior. |
| dotNetRDF | Federation/metadata graph experiments | unknown | Remove or gate unless concrete call sites exist. |
| Dapper | Direct DB access | unknown | Confirm call sites; prefer one DB access pattern unless justified. |
| MessagePack | Mesh/protocol serialization | experimental-feature | Gate under protocol features. |
| System.Reactive | Event/reactive flows | unknown | Confirm call sites. |
| Microsoft.Build.* | Custom build tasks | build-only | Move out of runtime app project if tasks remain. |
| Microsoft.CodeAnalysis.* | Static analysis/build tooling | build-only | Move out of runtime app project if tasks remain. |

## Release rule

No dependency should remain `unknown` when a feature is promoted to `stable`. Experimental-only dependencies should be disabled by default and documented in `FEATURE_INVENTORY.md`.
