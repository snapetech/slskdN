#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

if rg -n '\$\{item\.lastSearchId\}' "$repo_root/src/web/src/components/Wishlist/Wishlist.jsx" >&2; then
  printf 'Wishlist lastSearchId route segment must use encodeURIComponent\n' >&2
  failed=1
fi

if rg -nP '/searches/\$\{(?!(encodeURIComponent|segment)\()' "$repo_root/src/web/src/components" -g '*.jsx' >&2; then
  printf 'Search route Link ids must be encoded as one route segment\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Web route segment links are encoded.\n'
