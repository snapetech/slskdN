#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

if rg -n 'git push origin master|--base "master"|--base master' "$repo_root/.github/workflows" -g '*.yml' -g '*.yaml' >&2; then
  printf 'GitHub workflows must target the active main branch, not master, for fork writes and PR bases.\n' >&2
  failed=1
fi

# Per the license rollback (issue #221), slskdN no longer syncs from upstream slskd at all.
# The upstream sync/release workflows were deleted; assert they stay deleted so they can
# never be reintroduced as a one-click path to merge/track post-0.25.0 upstream code.
for forbidden in upstream-sync.yml upstream-release.yml; do
  if [ -e "$repo_root/.github/workflows/$forbidden" ]; then
    printf '%s must not exist: slskdN does not sync from upstream slskd (license rollback, issue #221).\n' "$forbidden" >&2
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'GitHub workflow branch targets match main-only fork automation policy.\n'
