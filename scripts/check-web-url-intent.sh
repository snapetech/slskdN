#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

while IFS= read -r file; do
  findings=$(perl -0ne '
    while (/navigate\s*\(\s*["\x27`]\/(browse|users|chat)["\x27`]\s*,\s*\{[^)]*state\s*:\s*\{\s*user\b/sg) {
      print "$ARGV: navigate to Browse/Users/Chat with user router state must include ?user= in the URL\n";
    }
    while (/to\s*=\s*\{\s*\{\s*pathname\s*:\s*["\x27`]\/(browse|users|chat)["\x27`]\s*,[^}]*state\s*:\s*\{\s*user\b/sg) {
      print "$ARGV: Link to Browse/Users/Chat with user router state must include ?user= in the URL\n";
    }
  ' "$file")

  if [ -n "$findings" ]; then
    printf '%s' "$findings" >&2
    failed=1
  fi
done < <(find "$repo_root/src/web/src" -type f \( -name '*.js' -o -name '*.jsx' \) | sort)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Browse, Users, and Chat user-intent links must be URL-addressable.
Router state may be kept as a fast path, but it cannot be the only carrier for a username.
MSG
  exit 1
fi

printf 'User-targeted Browse/Users/Chat navigation preserves URL intent.\n'
