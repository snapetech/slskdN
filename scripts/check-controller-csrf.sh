#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

while IFS= read -r file; do
  if rg -q '^[[:space:]]*\[Http(Post|Put|Delete|Patch)' "$file" && ! rg -q '^[[:space:]]*\[ValidateCsrfForCookiesOnly\]' "$file"; then
    printf '%s\n' "${file#$repo_root/}" >&2
    failed=1
  fi
done < <(find "$repo_root/src/slskd" \( -path '*/API/*Controller.cs' -o -name '*Controller.cs' \) -type f | sort)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Mutating controller actions require ValidateCsrfForCookiesOnly at controller scope.
MSG
  exit 1
fi

printf 'Mutating controllers have CSRF protection markers.\n'
