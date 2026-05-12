#!/usr/bin/env bash
set -euo pipefail

file="README.maturity.md"

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

echo "README maturity draft audit passed"
