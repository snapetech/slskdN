#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
allowlist="$repo_root/docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md"
failed=0

while IFS= read -r file; do
  rel="${file#$repo_root/}"
  if ! rg -q --fixed-strings "\`$rel\`" "$allowlist"; then
    printf '%s\n' "$rel" >&2
    failed=1
  fi
done < <(for f in $(find "$repo_root/src/slskd" \( -path '*/API/*Controller.cs' -o -name '*Controller.cs' \) -type f | sort); do rg -q '\[AllowAnonymous\]' "$f" && printf '%s\n' "$f"; done)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Controllers containing AllowAnonymous must be listed in docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md with a rationale.
MSG
  exit 1
fi

printf 'Anonymous endpoint controllers are documented in the allowlist.\n'
