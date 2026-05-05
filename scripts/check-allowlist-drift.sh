#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

check_allowlist_paths_exist() {
  local allowlist="$1"
  local label="$2"
  local stale
  stale="$(rg -o '`src/slskd/[^`]+Controller\.cs`' "$allowlist" | tr -d '`' | while IFS= read -r rel; do [ -f "$repo_root/$rel" ] || printf '%s\n' "$rel"; done)"
  if [ -n "$stale" ]; then
    printf '%s contains stale controller paths:\n%s\n' "$label" "$stale" >&2
    failed=1
  fi
}

check_allowlist_paths_exist "$repo_root/docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md" "ANONYMOUS_ENDPOINT_ALLOWLIST.md"
check_allowlist_paths_exist "$repo_root/docs/NON_VERSIONED_ROUTE_ALLOWLIST.md" "NON_VERSIONED_ROUTE_ALLOWLIST.md"

# Anonymous allowlist entries should still contain AllowAnonymous.
stale_anonymous="$(rg -o '`src/slskd/[^`]+Controller\.cs`' "$repo_root/docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md" | tr -d '`' | while IFS= read -r rel; do if [ -f "$repo_root/$rel" ] && ! rg -q '\[AllowAnonymous\]' "$repo_root/$rel"; then printf '%s\n' "$rel"; fi; done)"
if [ -n "$stale_anonymous" ]; then
  printf 'Anonymous allowlist entries no longer contain AllowAnonymous:\n%s\n' "$stale_anonymous" >&2
  failed=1
fi

# Non-versioned allowlist entries should still expose at least one non-versioned route.
stale_non_versioned="$(rg -o '`src/slskd/[^`]+Controller\.cs`' "$repo_root/docs/NON_VERSIONED_ROUTE_ALLOWLIST.md" | tr -d '`' | while IFS= read -r rel; do
  file="$repo_root/$rel"
  [ -f "$file" ] || continue
  routes="$(rg -n '\[Route\(' "$file" | sed 's/.*\[Route(//; s/)\].*//')"
  if [ -n "$routes" ] && ! printf '%s\n' "$routes" | rg -vq 'api/v\{version:apiVersion\}|api/v0|api/v1'; then
    printf '%s\n' "$rel"
  fi
done)"
if [ -n "$stale_non_versioned" ]; then
  printf 'Non-versioned allowlist entries no longer expose non-versioned routes:\n%s\n' "$stale_non_versioned" >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Endpoint allowlists match current controller state.\n'
