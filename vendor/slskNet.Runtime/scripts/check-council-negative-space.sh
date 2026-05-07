#!/usr/bin/env bash
#
# Negative-space gate for the bug council. Asserts two halves for every
# declared trust boundary in docs/dev/bug-council-negative-space.md:
#
#   1. assert_validator_present   — the validator symbol still exists in the
#                                   sink file. Catches "validator deleted."
#   2. assert_baseline_anchor     — a remediation-baseline check still
#                                   references the same anchor. Catches
#                                   "remediation gate silently removed."
#
# Both halves are required. The earlier (one-half) version of this gate was
# itself a council bug: a baseline pattern could be removed while the gate
# kept passing, because the gate only looked at the sink file. The fix
# mirrors slskdN's strengthening (assert_baseline_runs) into the runtime,
# adapted to the runtime's monolithic check-remediation-baseline.sh.
#
# Wired into scripts/check-remediation-baseline.sh; see
# docs/dev/bug-council-negative-space.md for the policy.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

failures=0

pass() { printf 'PASS %s\n' "$1"; }
fail() { printf 'FAIL %s\n' "$1" >&2; failures=$((failures + 1)); }

assert_validator_present() {
  local boundary="$1"
  local sink="$2"
  local symbol="$3"

  if [[ ! -e "$sink" ]]; then
    fail "negative-space: sink missing for boundary [$boundary]: $sink"
    return
  fi

  if rg -n --fixed-strings -- "$symbol" "$sink" >/dev/null; then
    pass "negative-space: [$boundary] $symbol present in $sink"
  else
    fail "negative-space: [$boundary] $symbol missing from $sink"
  fi
}

assert_baseline_anchor() {
  local boundary="$1"
  local anchor="$2"

  if rg -n --fixed-strings -- "$anchor" scripts/check-remediation-baseline.sh >/dev/null; then
    pass "negative-space: [$boundary] baseline anchor '$anchor' is registered"
  else
    fail "negative-space: [$boundary] baseline anchor '$anchor' is missing from check-remediation-baseline.sh"
  fi
}

# Server message frames (TCP from server)
assert_validator_present \
  "server-message-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "ValidateMessageLength"
assert_validator_present \
  "server-message-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "MaxMessageLength"
assert_baseline_anchor "server-message-frames" "ValidateMessageLength"
assert_baseline_anchor "server-message-frames" "MaxMessageLength"

# Init frames (TCP from peer)
assert_validator_present \
  "init-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "ValidateInitMessageLength"
assert_validator_present \
  "init-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "MaxInitMessageLength"
assert_baseline_anchor "init-frames" "ValidateInitMessageLength"
assert_baseline_anchor "init-frames" "MaxInitMessageLength"

# Buffered network reads
assert_validator_present \
  "buffered-network-reads" \
  "src/Network/Tcp/Connection.cs" \
  "MaximumBufferedReadLength"
assert_baseline_anchor "buffered-network-reads" "MaximumBufferedReadLength"

# Server protocol counts
assert_validator_present \
  "server-protocol-counts" \
  "src/Messaging/Messages/Server/ProtocolCountReader.cs" \
  "ReadValidatedCount"
assert_baseline_anchor "server-protocol-counts" "ReadValidatedCount"

# Server endpoint ports
assert_validator_present \
  "server-endpoint-ports" \
  "src/Messaging/Messages/ProtocolValueValidator.cs" \
  "ValidatePort"
assert_baseline_anchor "server-endpoint-ports" "ValidatePort"

# Peer transfer file sizes
assert_validator_present \
  "peer-transfer-file-sizes" \
  "src/Messaging/Messages/Peer/TransferRequest.cs" \
  "Invalid transfer file size"
assert_validator_present \
  "peer-transfer-file-sizes" \
  "src/Messaging/Messages/Peer/TransferResponse.cs" \
  "Invalid transfer file size"
assert_baseline_anchor "peer-transfer-file-sizes" "Invalid transfer file size"

# Distributed branch level
assert_validator_present \
  "distributed-branch-level" \
  "src/Messaging/Messages/Distributed/DistributedBranchLevel.cs" \
  "branch level"
assert_baseline_anchor "distributed-branch-level" "branch level"

# Distributed child depth
assert_validator_present \
  "distributed-child-depth" \
  "src/Messaging/Messages/Distributed/DistributedChildDepth.cs" \
  "child depth"
assert_baseline_anchor "distributed-child-depth" "child depth"

# Resolver outputs (anchor: rejection diagnostic when resolver output is
# invalid; the validator is the negative path of resolver dispatch).
assert_validator_present \
  "resolver-outputs" \
  "src/Messaging/Handlers/PeerMessageHandler.cs" \
  "Failed to send directory contents response"
assert_baseline_anchor "resolver-outputs" "Failed to send directory contents response"
assert_baseline_anchor "resolver-outputs" "Creates_Diagnostic_On_Invalid_FolderContentsResponse_Resolver_Output"

# Mythos-level analyzer (CSL0001) — the analyzer itself is a boundary in the
# sense that its absence silently disables the highest-severity lens.
assert_validator_present \
  "csl0001-taint-to-allocation" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToAllocationAnalyzer.cs" \
  "CSL0001"
assert_baseline_anchor "csl0001-taint-to-allocation" "CSL0001"

# Mythos-level analyzer (CSL0002) - loop-count DoS lens.
assert_validator_present \
  "csl0002-taint-to-loop-bound" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToLoopBoundAnalyzer.cs" \
  "CSL0002"
assert_baseline_anchor "csl0002-taint-to-loop-bound" "CSL0002"

# Mythos-level analyzer (CSL0003) - stream-position/skip lens.
assert_validator_present \
  "csl0003-taint-to-stream-position" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToStreamPositionAnalyzer.cs" \
  "CSL0003"
assert_baseline_anchor "csl0003-taint-to-stream-position" "CSL0003"

# Protocol fuzz harness — same logic; absence silently disables coverage.
assert_validator_present \
  "protocol-fuzz-harness" \
  "tests/Soulseek.Tests.Unit/Messaging/Fuzz/ProtocolAdversarialFuzz.cs" \
  "IsDocumentedFailure"
assert_baseline_anchor "protocol-fuzz-harness" "IsDocumentedFailure"
assert_baseline_anchor "protocol-fuzz-harness" "KnownCorpus"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d negative-space gate check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll negative-space gate checks passed.\n'
