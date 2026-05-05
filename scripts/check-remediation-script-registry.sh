#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
baseline="$repo_root/scripts/check-remediation-baseline.sh"
failed=0

while IFS= read -r script; do
  rel="${script#$repo_root/}"
  [ "$rel" = "scripts/check-remediation-baseline.sh" ] && continue

  if [ ! -x "$script" ]; then
    printf '%s is not executable\n' "$rel" >&2
    failed=1
  fi

  if ! rg -q --fixed-strings "\$repo_root/${rel}" "$baseline"; then
    printf '%s is not referenced by scripts/check-remediation-baseline.sh\n' "$rel" >&2
    failed=1
  fi
done < <(find "$repo_root/scripts" -maxdepth 1 -type f -name 'check-*.sh' | sort)

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Remediation check scripts are executable and registered.\n'
