#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

if rg -n 'git push origin master|--base "master"|--base master' "$repo_root/.github/workflows" -g '*.yml' -g '*.yaml' >&2; then
  printf 'GitHub workflows must target the active main branch, not master, for fork writes and PR bases.\n' >&2
  failed=1
fi

if rg -n '^\s*schedule:' "$repo_root/.github/workflows/upstream-sync.yml" >&2; then
  printf 'upstream-sync.yml must remain manual-only; scheduled upstream merges can push unreviewed branch changes.\n' >&2
  failed=1
fi

if [ "$(rg -n '^\s*- name: Create Issue on Conflict$' "$repo_root/.github/workflows/upstream-sync.yml" | wc -l)" -ne 1 ]; then
  printf 'upstream-sync.yml must contain exactly one Create Issue on Conflict step.\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'GitHub workflow branch targets match main-only fork automation policy.\n'
