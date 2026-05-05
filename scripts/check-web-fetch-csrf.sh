#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

while IFS= read -r file; do
  findings="$(perl -0ne '
    while (/fetch\s*\((.*?\n\s*\);)/sg) {
      my $call = $&;
      next unless $call =~ /method:\s*["\x27`](POST|PUT|DELETE|PATCH)["\x27`]/i;
      next unless $call =~ /session\.authHeaders\s*\(\s*\)/;
      next if $call =~ /session\.authHeaders\s*\(\s*\{\s*csrf\s*:\s*true\s*\}\s*\)/;
      print "$ARGV: mutating fetch uses session.authHeaders() without csrf opt-in\n";
    }
  ' "$file")"

  if [ -n "$findings" ]; then
    printf '%s' "$findings" >&2
    failed=1
  fi
done < <(find "$repo_root/src/web/src" -type f \( -name '*.js' -o -name '*.jsx' \) | sort)

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Mutating direct fetch wrappers must call session.authHeaders({ csrf: true }).
Shared axios API calls add CSRF automatically; this check only targets direct fetch usage.
MSG
  exit 1
fi

printf 'Mutating direct fetch calls opt into CSRF headers.\n'
