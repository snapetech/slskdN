#!/usr/bin/env bash
set -euo pipefail

failures=0

implemented="docs/security/implemented-security.md"
roadmap="docs/security/security-roadmap.md"
non_goals="docs/security/security-non-goals.md"
readme_draft="README.maturity.md"

for file in "$implemented" "$roadmap" "$non_goals" "$readme_draft"; do
  if [[ ! -f "$file" ]]; then
    echo "missing required file: $file" >&2
    failures=$((failures + 1))
  fi
done

roadmap_terms=(
  "NetworkGuard"
  "PeerReputation"
  "CryptographicCommitment"
  "ProofOfStorage"
  "ByzantineConsensus"
  "Honeypots"
  "Canary traps"
  "Entropy monitoring"
  "Paranoid mode"
)

if [[ -f "$roadmap" ]]; then
  for term in "${roadmap_terms[@]}"; do
    if ! grep -q "$term" "$roadmap"; then
      echo "$roadmap missing roadmap-only term: $term" >&2
      failures=$((failures + 1))
    fi
  done
fi

if [[ -f "$non_goals" ]]; then
  for term in "Byzantine" "Proof-of-storage" "Cryptographic" "peer reputation" "honeypot" "Entropy" "Paranoid"; do
    if ! grep -qi "$term" "$non_goals"; then
      echo "$non_goals missing non-goal coverage for: $term" >&2
      failures=$((failures + 1))
    fi
  done
fi

if [[ -f "$implemented" ]]; then
  forbidden_regex="NetworkGuard|PeerReputation|CryptographicCommitment|ProofOfStorage|ByzantineConsensus|Honeypots|Canary traps|Entropy monitoring|Paranoid mode"
  if grep -Eq "$forbidden_regex" "$implemented"; then
    echo "$implemented contains roadmap-only claim language" >&2
    failures=$((failures + 1))
  fi
fi

if [[ -f "$readme_draft" ]]; then
  if ! grep -q "Roadmap-only security claims" "$readme_draft"; then
    echo "$readme_draft missing roadmap-only security section" >&2
    failures=$((failures + 1))
  fi

  if ! grep -q "A feature is not stable merely because" "$readme_draft"; then
    echo "$readme_draft missing stability rule" >&2
    failures=$((failures + 1))
  fi
fi

if [[ "$failures" -ne 0 ]]; then
  echo "roadmap claim audit failed with $failures issue(s)" >&2
  exit 1
fi

echo "roadmap claim audit passed"
