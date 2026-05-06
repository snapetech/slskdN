#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

if rg -n 'return (BadRequest|Problem|StatusCode)\([^;]*ex\.Message|return Content\(BuildCallbackHtml\(\$"[^"]*\{error\}' \
  "$repo_root/src/slskd" \
  -g '*Controller.cs' \
  -g '!wwwroot/**' >&2; then
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Controller responses must not return raw exception messages or reflect OAuth error
query values into response bodies. Return stable client-facing error text and log
details server-side when details are needed.
MSG
  exit 1
fi

printf 'Controller exception responses use stable client-facing messages.\n'
