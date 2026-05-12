#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

failures=0
warnings=0

check_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "missing required file: $path" >&2
    failures=$((failures + 1))
  fi
}

warn_missing_readme_pointer() {
  local target="$1"
  if [[ -f README.md ]] && ! grep -q "$target" README.md; then
    echo "warning: README.md does not point to $target yet" >&2
    warnings=$((warnings + 1))
  fi
}

check_file "FEATURE_INVENTORY.md"
check_file "docs/status.md"
check_file "docs/security/implemented-security.md"
check_file "docs/security/security-roadmap.md"
check_file "docs/security/security-non-goals.md"
check_file "docs/dependencies.md"
check_file "docs/analyzer-suppressions.md"

# README is intentionally warning-only for this first branch because the current
# README is very large. The README maturity rewrite should be a separate patch
# that can be reviewed without hiding the docs/CI scaffolding in a giant diff.
warn_missing_readme_pointer "FEATURE_INVENTORY.md"
warn_missing_readme_pointer "docs/status.md"

if [[ -f FEATURE_INVENTORY.md ]]; then
  for term in \
    "NetworkGuard" \
    "PeerReputation" \
    "CryptographicCommitment" \
    "ProofOfStorage" \
    "ByzantineConsensus" \
    "Hash-from-audio-file" \
    "HashFromAudioFileEnabled" \
    "slskr"; do
    if ! grep -q "$term" FEATURE_INVENTORY.md; then
      echo "FEATURE_INVENTORY.md missing required term: $term" >&2
      failures=$((failures + 1))
    fi
  done
fi

if [[ -f docs/security/security-roadmap.md ]]; then
  for term in \
    "NetworkGuard" \
    "PeerReputation" \
    "CryptographicCommitment" \
    "ProofOfStorage" \
    "ByzantineConsensus"; do
    if ! grep -q "$term" docs/security/security-roadmap.md; then
      echo "security-roadmap.md missing roadmap term: $term" >&2
      failures=$((failures + 1))
    fi
  done
fi

if [[ -f docs/security/implemented-security.md ]]; then
  if grep -q "ByzantineConsensus\|ProofOfStorage\|CryptographicCommitment" docs/security/implemented-security.md; then
    echo "implemented-security.md mentions roadmap-only protocol guarantees" >&2
    failures=$((failures + 1))
  fi
fi

if [[ -f src/slskd/slskd.csproj && -f docs/analyzer-suppressions.md ]]; then
  while IFS= read -r nowarn_line; do
    [[ -z "$nowarn_line" ]] && continue
    IFS=';' read -ra suppressed <<< "$(printf '%s' "$nowarn_line" | sed -e 's#<NoWarn>##' -e 's#</NoWarn>##')"
    for warning in "${suppressed[@]}"; do
      [[ -z "$warning" ]] && continue
      if ! grep -q "\`$warning\`" docs/analyzer-suppressions.md; then
        echo "docs/analyzer-suppressions.md missing NoWarn entry: $warning" >&2
        failures=$((failures + 1))
      fi
    done
  done < <(grep -o "<NoWarn>[^<]*</NoWarn>" src/slskd/slskd.csproj || true)
fi

if [[ "$failures" -ne 0 ]]; then
  echo "feature coherence audit failed with $failures issue(s) and $warnings warning(s)" >&2
  exit 1
fi

echo "feature coherence audit passed with $warnings warning(s)"
