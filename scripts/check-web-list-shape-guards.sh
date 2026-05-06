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

if rg -n 'searchesEvent\.reduce' "$repo_root/src/web/src/components/Search/Searches.jsx" >&2; then
  printf 'Searches hub list events must be normalized before reduce\n' >&2
  failed=1
fi

if rg -n 'return Array\.isArray\(parsed\.panels\) \? parsed\.panels : \[\]' "$repo_root/src/web/src/components/Messaging/Messaging.jsx" >&2; then
  printf 'Messaging persisted panels must normalize each panel entry before rendering\n' >&2
  failed=1
fi

if rg -n 'Object\.entries\(counts \|\| \{\}\)' "$repo_root/src/web/src/lib/discoveryGraph.js" >&2; then
  printf 'Discovery Graph count maps must be guarded before Object.entries\n' >&2
  failed=1
fi

if rg -n 'Object\.entries\(lane\.metrics \|\| \{\}\)' "$repo_root/src/web/src/components/Search/SongIDPanel.jsx" >&2; then
  printf 'SongID lane metrics must be guarded before Object.entries\n' >&2
  failed=1
fi

if rg -n 'Object\.entries\((stats\.mappingsByDomain|lastSeenTimestamps|memberAffinities) \|\| \{\}\)' "$repo_root/src/web/src/components/System/MediaCore/index.jsx" >&2; then
  printf 'MediaCore map payloads must be guarded before Object.entries\n' >&2
  failed=1
fi

if rg -n '\(preset\.(shapes|sprites|waves) \|\| \[\]\)\.map' "$repo_root/src/web/src/components/Player/visualizers/nativeMilkdropEngine.js" >&2; then
  printf 'Native MilkDrop preset lists must be guarded before map\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Web list-shape render guards are present.\n'
