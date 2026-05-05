#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

matches="$(rg -n "api\.(get|post|put|delete|patch)\(('|\")/api" "$repo_root/src/web/src" -g '*.{js,jsx}' || true)"

if [ -n "$matches" ]; then
  cat >&2 <<'MSG'
Shared web API client calls must use paths relative to /api/v0.
Do not pass /api or /api/v0 into api.get/post/put/delete/patch.
MSG
  printf '%s\n' "$matches" >&2
  exit 1
fi

printf 'Web API client paths are relative to the shared /api/v0 base.\n'
