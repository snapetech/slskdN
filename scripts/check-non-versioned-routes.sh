#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
allowlist="$repo_root/docs/NON_VERSIONED_ROUTE_ALLOWLIST.md"
failed=0

while IFS= read -r file; do
  rel="${file#$repo_root/}"
  routes="$(rg -n '\[Route\(' "$file" | sed 's/.*\[Route(//; s/)\].*//')"
  [ -n "$routes" ] || continue

  if ! printf '%s\n' "$routes" | rg -vq 'api/v\{version:apiVersion\}|api/v0|api/v1'; then
    continue
  fi

  if ! rg -q --fixed-strings "\`$rel\`" "$allowlist"; then
    printf '%s\n' "$rel" >&2
    failed=1
  fi
done < <(find "$repo_root/src/slskd" \( -path '*/API/*Controller.cs' -o -name '*Controller.cs' \) -type f | sort)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Non-versioned controllers must either expose a versioned alias or be listed in docs/NON_VERSIONED_ROUTE_ALLOWLIST.md with a rationale.
MSG
  exit 1
fi

printf 'Non-versioned controllers are versioned or documented.\n'
