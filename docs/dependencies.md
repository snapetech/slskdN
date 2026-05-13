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
| TagLibSharp | HashDb media attribute probing (`HashDbService`) and audio/library surfaces | required-feature | Keep because active share/media probing call sites exist; ensure slow/remote storage opt-out remains documented. |
| FluentFTP | FTP integration | experimental-feature | Gate integration and document credentials/egress. |
| Mono.Nat | NAT/port mapping | experimental-feature | Gate under mesh/VPN/network features. |
| MonoTorrent | DHT rendezvous and BitTorrent-backed swarm experiments | experimental-feature | Gated by DHT/multi-source surfaces; confirm public bootstrap policy in runtime docs. |
| NSec.Cryptography | Mesh transport signing and ActivityPub key/signature work | experimental-feature | Keep with mesh/social federation gates; expand protocol tests before stable claims. |
| MathNet.Numerics | Ranking/SongID/analytics math | unknown | No active call site found in `src/slskd`; remove unless a concrete call site appears. |
| AWSSDK.S3 | VirtualSoulfind v2 S3 backend | experimental-feature | Gated by VirtualSoulfind; document credential/egress behavior before stable claims. |
| Zeroconf | Identity/Friends LAN discovery (`LanDiscoveryService`) | experimental-feature | Gate via identity/friends or discovery posture; document multicast behavior. |
| dotNetRDF | Solid/WebID roadmap/docs | unused | No active `VDS.RDF` call site found in `src/slskd`; remove unless Solid implementation lands. |
| Dapper | VirtualSoulfind v2 SQLite catalogue store | experimental-feature | Gated by VirtualSoulfind; acceptable while catalogue store remains active. |
| MessagePack | Mesh/protocol serialization | experimental-feature | Gate under protocol features. |
| System.Reactive | VirtualSoulfind disaster-mode transfer progress subjects | experimental-feature | Gated by VirtualSoulfind; keep while `MeshTransferService` uses `Subject<T>`. |
| Microsoft.Build.* | Custom build tasks | build-only | Move out of runtime app project if tasks remain. |
| Microsoft.CodeAnalysis.* | Static analysis/build tooling | build-only | Move out of runtime app project if tasks remain. |

## Release rule

No dependency should remain `unknown` when a feature is promoted to `stable`. Experimental-only dependencies should be feature-gated and documented in `FEATURE_INVENTORY.md`.
