# Documentation Audit - 2026-05-05

This audit compares the current repository documentation against the latest runtime sync, route-versioning, federation diagnostics, Library Health, and System UI changes. It focuses on actionable gaps rather than historical archive cleanup.

## Summary

The top-level README and primary references now cover the new `slskNet.Runtime` capability/rendezvous work at a high level. The remaining gaps are mostly depth and consistency problems: several areas have implementation notes or tests, but no stable user-facing guide; route inventory exists, but is not yet enforced as a generated artifact; and some older docs still describe removed review surfaces.

## Completed In This Pass

| Area | Update |
| --- | --- |
| README | Added runtime capability handshakes, Soulseek mesh rendezvous, System -> Mesh discovery tools, config posture, status table rows, runtime sync link, audit link, and permissive dependency note. |
| API docs | Added `/api/v0/soulseek/mesh-rendezvous/*` and `/api/v0/soulseek/peer-capabilities` endpoints with response shape and privacy caveat. |
| Native discovery guide | Added rendezvous/capability features, API list, UI integration, compatibility fallback, and safety limiter notes. |
| Config docs | Added `mesh.enable_soulseek_capability_handshake`, `mesh.enable_soulseek_rendezvous`, and `mesh.probe_soulseek_rendezvous_capabilities`. |
| Example config | Added commented mesh rendezvous/capability options. |
| System surfaces | Added System -> Mesh rendezvous and runtime capability coverage. |

## Priority Gaps

| Priority | Gap | Evidence | Remediation |
| --- | --- | --- | --- |
| Done | Route inventory is checked before release packaging. | `scripts/check-route-inventory.sh` and `scripts/check-remediation-baseline.sh` compare generated route inventory against `docs/system-surfaces-current.md`. | Keep the release gate wired to the remediation baseline checks. |
| Done | Full vendored runtime test expectations are documented in the main testing policy. | `docs/dev/testing-policy.md` lists focused app-side runtime tests, the vendored runtime suite, and baseline-caveat handling. | Keep this section current when the vendored runtime test baseline changes. |
| Done | System -> Integrations federation diagnostics has a user-facing doc. | `docs/federation-diagnostics.md` and `docs/system-surfaces.md` describe read-only checks and privacy posture. | Keep the guide aligned with diagnostics fields. |
| P1 | Library Health versioned alias changes are documented in route policy but not in user/API workflow depth. | `docs/api-route-versioning-policy.md` notes the alias tranche; API docs list Library Health but do not explain legacy versus versioned client behavior. | Add alias notes under Library Health API docs and link to route policy. |
| P1 | Soulseek mesh rendezvous has privacy notes, but no troubleshooting section. | README/config/native-discovery cover opt-in; troubleshooting does not cover disabled controls, rate limits, no candidates, or missing signed descriptors. | Add a troubleshooting subsection for System -> Mesh rendezvous failures. |
| Done | Runtime capability descriptor format is documented beyond the conceptual level. | `docs/slsknet-runtime-sync.md` now defines descriptor fields, known feature strings, trust rules, and fallback behavior. | Keep feature strings current with vendored runtime additions. |
| P2 | README is feature-heavy and risks burying operational warnings. | New runtime features were added in existing sections, but privacy/security warnings are split across README, config, and native discovery guide. | Consider a compact "Network exposure and privacy quick reference" in README that links to `docs/network-privacy-security-surfaces.md`. |
| P2 | Archive/planning docs include stale references to removed Discovery Inbox promotion paths. | Changelog and active docs mention removed paths; archive docs still contain old design language. | Leave archived files intact, but add a prominent archive disclaimer to `docs/archive/README.md` or equivalent index if one exists. |
| P2 | API documentation is still hand-maintained. | `docs/api-documentation.md` relies on manual endpoint lists. | Generate API route tables from controller attributes and link stable user docs to generated route inventory. |
| P2 | Feature status table does not map to config flags. | README status table names features but does not point to exact config keys for experimental features. | Add a compact config-key column or link each experimental feature to config docs. |

## Suggested Remediation Order

1. Add System -> Mesh troubleshooting for rendezvous disabled/rate-limited/no candidates/no signed descriptors.
2. Add Library Health route alias notes to API docs.
3. Add an archive disclaimer so stale planning material is clearly historical.
4. Generate API route tables from controller attributes and link stable user docs to generated route inventory.

## Documentation Health Checks To Keep Running

- `rg -n "Discovery Inbox|Wishlist Import Feed|promotion" README.md docs/*.md docs/dev docs/design`
- `rg -n "mesh-rendezvous|peer-capabilities|slskdn-mesh-v1|enable_soulseek" README.md docs config/slskd.example.yml`
- `bash scripts/generate-route-inventory.sh`
- `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj`
- `npm test -- --run src/lib/soulseekDiscovery.test.js src/components/System/Mesh/index.test.jsx`
