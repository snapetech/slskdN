#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

require_poll_guard() {
  local file="$1"
  local path="$repo_root/$file"

  if ! rg -q --fixed-strings 'setInterval(' "$path"; then
    return
  fi

  if ! rg -q --fixed-strings 'const mountedRef = useRef(true);' "$path"; then
    printf '%s starts polling without a mountedRef lifecycle guard\n' "$file" >&2
    failed=1
  fi

  if ! rg -q --fixed-strings 'if (!mountedRef.current) return;' "$path"; then
    printf '%s starts polling without guarding async completion before state updates\n' "$file" >&2
    failed=1
  fi

  if ! rg -q --fixed-strings 'mountedRef.current = false;' "$path"; then
    printf '%s starts polling without marking unmount before async completions resolve\n' "$file" >&2
    failed=1
  fi
}

require_poll_guard "src/web/src/components/System/Network/index.jsx"
require_poll_guard "src/web/src/components/System/Mesh/index.jsx"
require_poll_guard "src/web/src/components/System/SwarmVisualization/index.jsx"
require_poll_guard "src/web/src/components/System/SwarmAnalytics/index.jsx"
require_poll_guard "src/web/src/components/System/Jobs/index.jsx"
require_poll_guard "src/web/src/components/System/Security/index.jsx"
require_poll_guard "src/web/src/components/System/Bridge/index.jsx"
require_poll_guard "src/web/src/components/System/MediaCore/index.jsx"

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Web polling lifecycle checks passed.\n'
