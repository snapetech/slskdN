#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

while IFS= read -r file; do
  if ! awk '
    BEGIN { found = 0 }
    /\[Http(Post|Put|Patch|Delete)/ {
      http_line = NR
      block = $0 "\n"
      for (i = 0; i < 12 && getline line > 0; i++) {
        block = block line "\n"
        if (line ~ /public .*Task<.*IActionResult|public .*IActionResult|public .*Task</) {
          break
        }
      }
      if (block ~ /\[AllowAnonymous\]/) {
        next
      }
      if (block !~ /\[Authorize\([^]]*Roles[[:space:]]*=/) {
        printf "%s:%d: mutating action must declare method-level Authorize Roles\n", FILENAME, http_line
        found = 1
      }
    }
    END { exit found ? 1 : 0 }
  ' "$file" | sed "s#^$repo_root/##" >&2; then
    failed=1
  fi
done < <(find "$repo_root/src/slskd" \( -path '*/API/*Controller.cs' -o -name '*Controller.cs' \) -type f | sort)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Mutating controller actions require an explicit method-level role requirement.
Class-level authentication does not prove read/write authorization.
MSG
  exit 1
fi

printf 'Mutating controller actions declare explicit role requirements.\n'
