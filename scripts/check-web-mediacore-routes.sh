#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
file="$repo_root/src/web/src/lib/mediacore.js"
failed=0

bad_podcore="$(rg -n '/mediacore/podcore|apiBaseUrl' "$file" || true)"
if [ -n "$bad_podcore" ]; then
  cat >&2 <<'MSG'
MediaCore web helpers must use relative /podcore/* paths through the shared /api/v0 client.
Do not use /mediacore/podcore/* or apiBaseUrl in src/web/src/lib/mediacore.js.
MSG
  printf '%s\n' "$bad_podcore" >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'MediaCore web pod routes use relative shared-client paths.\n'
