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

# Mythos-level analyzer (CSL0004) - file/directory path sink lens.
assert_validator_present \
  "csl0004-taint-to-file-path" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToFilePathAnalyzer.cs" \
  "CSL0004"
assert_baseline_anchor "csl0004-taint-to-file-path" "CSL0004"

# Mythos-level analyzers (CSL0005-CSL0008) - batched semantic sink lenses.
assert_validator_present \
  "csl0005-taint-to-timeout" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToTimeoutAnalyzer.cs" \
  "CSL0005"
assert_baseline_anchor "csl0005-taint-to-timeout" "CSL0005"

assert_validator_present \
  "csl0006-taint-to-endpoint" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToEndpointAnalyzer.cs" \
  "CSL0006"
assert_baseline_anchor "csl0006-taint-to-endpoint" "CSL0006"

assert_validator_present \
  "csl0007-taint-to-enum" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToEnumAnalyzer.cs" \
  "CSL0007"
assert_baseline_anchor "csl0007-taint-to-enum" "CSL0007"

assert_validator_present \
  "csl0008-taint-to-string-slice" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToStringSliceAnalyzer.cs" \
  "CSL0008"
assert_baseline_anchor "csl0008-taint-to-string-slice" "CSL0008"

# Mythos-level analyzers (CSL0009-CSL0016) - full runtime semantic sink batch.
assert_validator_present \
  "csl0009-taint-to-diagnostic" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToDiagnosticAnalyzer.cs" \
  "CSL0009"
assert_baseline_anchor "csl0009-taint-to-diagnostic" "CSL0009"

assert_validator_present \
  "csl0010-taint-to-message-builder" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToMessageBuilderAnalyzer.cs" \
  "CSL0010"
assert_baseline_anchor "csl0010-taint-to-message-builder" "CSL0010"

assert_validator_present \
  "csl0011-taint-to-cache-key" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToCacheKeyAnalyzer.cs" \
  "CSL0011"
assert_baseline_anchor "csl0011-taint-to-cache-key" "CSL0011"

assert_validator_present \
  "csl0012-taint-to-crypto-trust" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToCryptoTrustAnalyzer.cs" \
  "CSL0012"
assert_baseline_anchor "csl0012-taint-to-crypto-trust" "CSL0012"

assert_validator_present \
  "csl0013-taint-to-dynamic-execution" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToDynamicExecutionAnalyzer.cs" \
  "CSL0013"
assert_baseline_anchor "csl0013-taint-to-dynamic-execution" "CSL0013"

assert_validator_present \
  "csl0014-taint-to-parser-runtime" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToParserRuntimeAnalyzer.cs" \
  "CSL0014"
assert_baseline_anchor "csl0014-taint-to-parser-runtime" "CSL0014"

assert_validator_present \
  "csl0015-taint-to-resource-capacity" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToResourceCapacityAnalyzer.cs" \
  "CSL0015"
assert_baseline_anchor "csl0015-taint-to-resource-capacity" "CSL0015"

assert_validator_present \
  "csl0016-taint-to-buffer-operation" \
  "analyzers/Soulseek.CouncilAnalyzers/TaintToBufferOperationAnalyzer.cs" \
  "CSL0016"
assert_baseline_anchor "csl0016-taint-to-buffer-operation" "CSL0016"

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
