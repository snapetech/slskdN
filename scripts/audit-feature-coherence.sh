#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

failures=0

check_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "missing required file: $path" >&2
    failures=$((failures + 1))
  fi
}

check_file "FEATURE_INVENTORY.md"
check_file "docs/status.md"
check_file "docs/security/implemented-security.md"
check_file "docs/security/security-roadmap.md"
check_file "docs/security/security-non-goals.md"
check_file "docs/dependencies.md"
check_file "docs/analyzer-suppressions.md"

if [[ -f README.md ]]; then
  if ! grep -q "FEATURE_INVENTORY.md" README.md; then
    echo "README.md does not point to FEATURE_INVENTORY.md" >&2
    failures=$((failures + 1))
  fi

  if ! grep -q "docs/status.md" README.md; then
    echo "README.md does not point to docs/status.md" >&2
    failures=$((failures + 1))
  fi
fi

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
  nowarn_line="$(grep -o "<NoWarn>[^<]*</NoWarn>" src/slskd/slskd.csproj | head -n 1 || true)"
  if [[ -n "$nowarn_line" ]]; then
    IFS=';' read -ra warnings <<< "$(printf '%s' "$nowarn_line" | sed -e 's#<NoWarn>##' -e 's#</NoWarn>##')"
    for warning in "${warnings[@]}"; do
      [[ -z "$warning" ]] && continue
      if ! grep -q "\`$warning\`" docs/analyzer-suppressions.md; then
        echo "docs/analyzer-suppressions.md missing NoWarn entry: $warning" >&2
        failures=$((failures + 1))
      fi
    done
  fi
fi

if [[ "$failures" -ne 0 ]]; then
  echo "feature coherence audit failed with $failures issue(s)" >&2
  exit 1
fi

echo "feature coherence audit passed"
