#!/usr/bin/env bash
set -euo pipefail

file="README.md"
draft="README.maturity.md"

if [[ ! -f "$file" ]]; then
  echo "missing $file" >&2
  exit 1
fi

for term in "FEATURE_INVENTORY.md" "docs/status.md" "implemented-security.md" "security-roadmap.md" "security-non-goals.md" "HashFromAudioFileEnabled" "Roadmap-only security claims"; do
  if ! grep -q "$term" "$file"; then
    echo "$file missing required term: $term" >&2
    exit 1
  fi
done

if [[ ! -f "$draft" ]] || ! cmp -s "$file" "$draft"; then
  echo "README.md and README.maturity.md must contain the same reviewed landing page" >&2
  exit 1
fi

echo "README maturity audit passed"
