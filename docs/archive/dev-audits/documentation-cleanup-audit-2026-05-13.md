# Documentation Cleanup Audit - 2026-05-13

This audit covers tracked markdown files, helper scripts, and workflow-facing
documentation in the repository. It records the cleanup applied on 2026-05-13
and the larger consolidation work that remains.

## Summary

Before cleanup, the repository had:

- 322 tracked markdown files.
- 119 active top-level `docs/` markdown files.
- 37 `docs/dev/` markdown files.
- 130 `docs/archive/` markdown files.
- 27 `memory-bank/` markdown files.
- 48 tracked shell scripts under `scripts/`.

The main issue was not just volume. Several active docs were historical
snapshots or one-off incident notes, while some archived onboarding docs still
looked like current instructions.

## Kept Active

These are current enough to remain active, though some still need normal
maintenance:

- `README.md`
- `CHANGELOG.md`
- `AGENTS.md`
- `CONTRIBUTING.md`
- `docs/README.md`
- `docs/getting-started.md`
- `docs/config.md`
- `docs/build.md`
- `docs/docker.md`
- `docs/troubleshooting.md`
- `docs/known_issues.md`
- `docs/status.md`
- `docs/FEATURES.md`
- `docs/HOW-IT-WORKS.md`
- `docs/api-documentation.md`
- `docs/api-route-versioning-policy.md`
- `docs/dev/LOCAL_DEVELOPMENT.md`
- `docs/dev/release-checklist.md`
- `docs/dev/testing-policy.md`
- `docs/dev/bugfix-verification-checklist.md`
- `docs/REMEDIATION_COMPLETION_REPORT.md`
- `docs/REMEDIATION_REVIEW_CHECKLIST.md`
- `docs/security/implemented-security.md`
- `docs/security/security-roadmap.md`
- `memory-bank/activeContext.md`
- `memory-bank/tasks.md`
- `memory-bank/progress.md`
- `memory-bank/decisions/adr-0001-known-gotchas.md`
- `memory-bank/decisions/adr-0002-code-patterns.md`
- `memory-bank/decisions/adr-0003-anti-slop-rules.md`
- `memory-bank/decisions/adr-0005-tagging-system.md`

## Archived In This Pass

These active docs read like dated snapshots, incident reports, or completed
workstream notes and were moved under `docs/archive/`:

- `docs/archive/status/2026-01/CURRENT_STATUS.md`
- `docs/archive/status/2026-01/next-steps-summary.md`
- `docs/archive/e2e/E2E_CORS_FIX.md`
- `docs/archive/e2e/E2E_FIXES_SUMMARY.md`
- `docs/archive/e2e/E2E_TEST_RESULTS.md`
- `docs/archive/e2e/e2e-file-verification-options.md`
- `docs/archive/e2e/e2e-startup-hang-analysis.md`
- `docs/archive/incidents/T916_NODE_EXIT_INVESTIGATION.md`
- `docs/archive/incidents/T-SF05-AUDIT.md`
- `docs/archive/test-plans/TEST_COVERAGE_ASSESSMENT.md`
- `docs/archive/test-plans/TEST_COVERAGE_SUMMARY.md`
- `docs/archive/dev-audits/backlog-verification-summary.md`
- `docs/archive/dev-audits/documentation-audit-2026-04-30.md`
- `docs/archive/dev-audits/documentation-audit-2026-05-05.md`
- `docs/archive/dev-audits/gitignore-artifact-audit-2026-04-30.md`
- `docs/archive/dev-audits/placeholder-completion-plan-2026-05-01.md`
- `docs/archive/dev-audits/placeholder-null-heavy-inventory.md`
- `docs/archive/dev-audits/slskd-tests-integration-audit.md`
- `docs/archive/dev-audits/slskd-tests-unit-completion-plan.md`
- `docs/archive/dev-audits/slskd-tests-unit-future-work.md`
- `docs/archive/dev-audits/slskd-tests-unit-lift-vs-requirements.md`
- `docs/archive/dev-audits/slskd-tests-unit-reenablement-execution-plan.md`
- `docs/archive/dev-audits/slskd-tests-unit-skips-how-to-fix.md`

## Consolidate Later

These appear to overlap enough that one canonical doc plus redirects would be
cleaner:

- `docs/system-surfaces.md` and `docs/system-surfaces-current.md`
- `docs/security-configuration.md` and `docs/security/implemented-security.md`
- `docs/SECURITY-GUIDELINES.md`, `docs/SECURITY_HARDENING_ROADMAP.md`, and
  `docs/security/security-roadmap.md`
- `docs/multi-swarm-architecture.md`, `docs/multi-swarm-roadmap.md`,
  `docs/MULTI_SWARM_IMPLEMENTATION_GUIDE.md`, and
  `memory-bank/multi-swarm-task-summary.md`
- `docs/virtualsoulfind-v2-design.md`,
  `docs/virtual-soulfind-mesh-architecture.md`, and
  `docs/VIRTUAL_SOULFIND_USER_GUIDE.md`
- `docs/pod-api-design.md`, `docs/pod-f1000-social-hub-design.md`,
  `docs/pod-identity-lifecycle.md`, and `docs/pods-and-rooms.md`
- `docs/TESTING-STRATEGY.md`, `docs/dev/testing-policy.md`, and
  `docs/dev/e2e-testing-guide.md`

## Root Cleanup

Moved:

- `docs/archive/security/SLSKDN-security-audit.feb26.md`
- `docs/archive/root/TODO.md`
- `docs/FORKING.md`

Kept at root:

- `FEATURE_INVENTORY.md`, because it remains canonical for public claims.
- `README.maturity.md`, because the feature-coherence workflow and audit
  scripts still treat it as a maintained README draft.

## Script Cleanup

Removed:

- `scripts/fix-aur-build-192.168.50.48.sh`, a host-specific stale workaround.
- `scripts/fix-python-torchaudio-no-resume.sh`, a local package workaround that
  should not be part of the release-maintained helper surface.

Keep:

- `scripts/create-release-tag.sh`
- `scripts/verify-github-target.sh`
- `scripts/verify-release-artifacts.sh`
- `scripts/check-remediation-baseline.sh`
- all currently registered `scripts/check-*.sh` files unless their checks are
  intentionally retired.

Review later:

- `scripts/audit-readme-maturity-draft.sh`
- `scripts/audit-roadmap-claims.sh`
- `scripts/run-share-scan-harness.sh`

## Archive Cleanup

Applied:

- Added `docs/archive/README.md` explaining that archived files are not current
  instructions.
- Moved stale active incident, E2E, test-plan, status, and dev-audit docs into
  archive folders.
- Updated active references that still point to moved files.

Still needed:

- Stop requiring `docs/archive/implementation/AI_START_HERE.md` as current
  onboarding, because it still references December branch names, old test
  counts, and obsolete next steps.

## Gaps To Fill

The active docs need fewer files and clearer owners:

- One current release-readiness page with exact required checks.
- One current local development page with backend/frontend/test commands.
- One current feature-claim policy that points to `FEATURE_INVENTORY.md`.
- One current operator troubleshooting page for Arch/AUR, service worker/cache,
  ports, downloads, and package switching.
- One current security status page that distinguishes implemented controls,
  design-only ideas, and retired plans.

## Remaining Order

1. Consolidate security docs into one active status page plus detailed archived
   plans.
2. Consolidate testing docs into one policy plus one E2E guide.
3. Review the remaining optional audit scripts and either wire them into release
   policy or archive them.
4. Continue updating `docs/README.md` whenever active docs are merged or moved.
