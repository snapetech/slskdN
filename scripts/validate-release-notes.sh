#!/usr/bin/env bash

set -euo pipefail

version="${1:-}"
notes_path="${2:-}"
changelog_path="${CHANGELOG_PATH:-docs/CHANGELOG.md}"

fail() {
  echo "[validate-release-notes] ERROR: $*" >&2
  exit 1
}

[[ -n "$version" && -n "$notes_path" ]] || fail "usage: $0 <logical-version> <notes-path>"
[[ -s "$notes_path" ]] || fail "release notes are missing or empty: $notes_path"
[[ -f "$changelog_path" ]] || fail "changelog not found: $changelog_path"

grep -Fqx "# slskdN $version" "$notes_path" || fail "notes title does not match $version"
grep -Eq '^Released: [0-9]{4}-[0-9]{2}-[0-9]{2}$' "$notes_path" || fail "notes do not contain a release date"
grep -Fqx '## Highlights' "$notes_path" || fail "notes do not contain a Highlights section"
grep -Fq "## [$version]" "$changelog_path" || fail "docs/CHANGELOG.md needs an exact section for $version"

if grep -Eqi 'No recorded changes|No changes|TODO|TBD|placeholder|Add release notes here' "$notes_path"; then
  fail "notes contain placeholder or empty-release wording"
fi

highlight_text="$(awk '
  /^## Highlights$/ { in_highlights = 1; next }
  in_highlights && /^## / { exit }
  in_highlights { print }
' "$notes_path")"

bullet_count="$(printf '%s\n' "$highlight_text" | grep -Ec '^- ' || true)"
meaningful_chars="$(printf '%s' "$highlight_text" | tr -d '[:space:]' | wc -c | tr -d ' ')"

(( bullet_count >= 1 )) || fail "Highlights must contain at least one bullet"
(( meaningful_chars >= 80 )) || fail "Highlights are too short to be informative (${meaningful_chars} non-space characters)"

echo "Release notes validated for $version: $bullet_count bullets, $meaningful_chars non-space highlight characters."
