#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

if rg -n 'setItems\(data\)' "$repo_root/src/web/src/components/Wishlist/Wishlist.jsx" >&2; then
  printf 'Wishlist must normalize API list payloads before storing render state\n' >&2
  failed=1
fi

if rg -n 'Object\.entries\(sharesByHost\)' "$repo_root/src/web/src/components/System/Shares/index.jsx" >&2; then
  printf 'System Shares must guard host-map payloads before Object.entries\n' >&2
  failed=1
fi

if rg -n 'sharesForHost\.map' "$repo_root/src/web/src/components/System/Shares/index.jsx" >&2; then
  printf 'System Shares must guard per-host share arrays before map\n' >&2
  failed=1
fi

if rg -n 'manifest\.items\.(map|reduce|filter|length)' "$repo_root/src/web/src/components/Shares/SharedWithMe.jsx" >&2; then
  printf 'SharedWithMe must guard manifest.items before list operations\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Web list-shape render guards are present.\n'
