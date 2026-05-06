#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

patterns=(
  '(^|[^A-Za-z0-9])sk-[A-Za-z0-9_-]{20,}'
  'xox[baprs]-[A-Za-z0-9-]{20,}'
  'ghp_[A-Za-z0-9]{30,}'
  'github_pat_[A-Za-z0-9_]{30,}'
  'AKIA[0-9A-Z]{16}'
  '-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----'
)

for pattern in "${patterns[@]}"; do
  matches="$(rg -n --hidden \
    -g '!vendor/**' \
    -g '!node_modules/**' \
    -g '!src/web/node_modules/**' \
    -g '!**/node_modules/**' \
    -g '!**/bin/**' \
    -g '!**/obj/**' \
    -g '!coverage/**' \
    -g '!**/build/**' \
    -g '!**/dist/**' \
    -- "$pattern" "$repo_root" 2>/dev/null || true)"
  if [ -n "$matches" ]; then
    printf 'Sensitive-looking token pattern found: %s\n%s\n' "$pattern" "$matches" >&2
    failed=1
  fi
done

if rg -n 'Generated CSRF token:|X-Slskdn-Csrf: \{Token\}|Cached (auth|share upload|file upload|file download) token \{Token\}|Pushbullet notification \{Title\} \{Body\}|Supplied credential \{Credential\}|expected credential \{Expected\}|VSF-BRIDGE-PROXY.*token: \{Token\}|VSF-BRIDGE-PROXY.*\{Query\}.*\{Token\}|VSF-BRIDGE-PROXY.*\{Username\}/\{Filename\}.*\{Token\}' \
  "$repo_root/src/slskd" >&2; then
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Remove real secrets from the repository or replace them with obvious placeholders.
MSG
  exit 1
fi

printf 'No high-confidence sensitive token patterns found.\n'
