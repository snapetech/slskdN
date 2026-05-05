#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

guarded_dirs=(
  "$repo_root/src/slskd/SourceFeeds"
  "$repo_root/src/slskd/SongID"
  "$repo_root/src/slskd/Solid"
  "$repo_root/src/slskd/VirtualSoulfind"
  "$repo_root/src/slskd/Relay"
  "$repo_root/src/slskd/Integrations/Webhooks"
)

while IFS= read -r file; do
  if rg -n 'AllowAutoRedirect\s*=\s*true' "$file" >&2; then
    failed=1
  fi

  if rg -q 'CreateClient\(\)|new HttpClient|new HttpClientHandler|SocketsHttpHandler' "$file"; then
    if ! rg -q 'NoRedirectHttpClientName|CreateGuardedNoRedirectClient|CreateNoRedirectHandler|AllowAutoRedirect\s*=\s*false' "$file"; then
      printf '%s: guarded outbound HTTP caller must use the no-redirect guarded client\n' "${file#$repo_root/}" >&2
      failed=1
    fi
  fi
done < <(find "${guarded_dirs[@]}" -type f -name '*.cs' | sort)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Guarded outbound HTTP features must bind URI validation to the actual request.
Use the shared no-redirect guarded client or validate every redirect hop explicitly.
MSG
  exit 1
fi

printf 'Guarded outbound HTTP callers use no-redirect guarded clients.\n'
