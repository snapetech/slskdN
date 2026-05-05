#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
expected="$repo_root/docs/system-surfaces-current.md"
tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT

"$repo_root/scripts/generate-route-inventory.sh" "$tmp" >/dev/null

normalize_inventory() {
  sed -E 's/^Generated: .*/Generated: <timestamp>/' "$1"
}

if ! diff -u <(normalize_inventory "$expected") <(normalize_inventory "$tmp"); then
  cat >&2 <<'MSG'

API route inventory is stale.
Run: scripts/generate-route-inventory.sh docs/system-surfaces-current.md
MSG
  exit 1
fi

printf 'API route inventory is current.\n'
