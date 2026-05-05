#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

failures=0

pass() {
  printf 'PASS %s\n' "$1"
}

fail() {
  printf 'FAIL %s\n' "$1" >&2
  failures=$((failures + 1))
}

require_file() {
  local path="$1"
  local label="$2"

  if [[ -f "$path" ]]; then
    pass "$label"
  else
    fail "$label: missing $path"
  fi
}

require_pattern() {
  local pattern="$1"
  local path="$2"
  local label="$3"

  if rg -n -U --pcre2 --hidden --glob '!.git/**' "$pattern" "$path" >/dev/null; then
    pass "$label"
  else
    fail "$label"
  fi
}

require_absent_pattern() {
  local pattern="$1"
  local path="$2"
  local label="$3"

  if rg -n -U --pcre2 --hidden --glob '!.git/**' "$pattern" "$path" >/tmp/slsknet-runtime-remediation-hit.$$ 2>/dev/null; then
    fail "$label"
    sed 's/^/  /' /tmp/slsknet-runtime-remediation-hit.$$ >&2
  else
    pass "$label"
  fi

  rm -f /tmp/slsknet-runtime-remediation-hit.$$
}

require_file "docs/dev/bug-burndown-ledger.md" "bug burndown ledger exists"
require_file "scripts/check-remediation-baseline.sh" "remediation baseline script exists"

require_pattern "ProtocolCountReader" "src/Messaging/Messages" "protocol parsers use centralized count reader"
require_pattern "ReadValidatedCount" "src/Messaging/Messages/Server/ProtocolCountReader.cs" "protocol count validation is centralized"
require_pattern "count < 0" "src/Messaging/Messages/Server/ProtocolCountReader.cs" "protocol count reader rejects negative counts"
require_pattern "count > maximumPossibleCount" "src/Messaging/Messages/Server/ProtocolCountReader.cs" "protocol count reader rejects impossible counts"
require_pattern "ValidateMatchingCount" "src/Messaging/Messages" "parallel protocol collection counts are matched"
require_pattern "ValidateNonNegativeCount" "src/Messaging/Messages/Server/RoomListResponseFactory.cs" "room list user counts reject negative values"
require_pattern "Invalid file size" "src/Messaging/MessageReaderExtensions.cs" "file parsers reject invalid negative sizes"
require_pattern "Invalid transfer file size" "src/Messaging/Messages/Peer" "transfer parsers reject invalid negative sizes"
require_pattern "ValidatePort" "src/Messaging/Messages/Server" "server endpoint parsers validate ports"
require_pattern "ValidateAdvertisedPort" "src/Messaging/Messages/Server" "obfuscated endpoint metadata validates advertised ports"
require_pattern "ProtocolCountHardeningTests" "tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolCountHardeningTests.cs" "protocol count regression tests are registered"

require_pattern "ValidateMessageLength" "src/Network" "message frame length validation is wired"
require_pattern "ValidateInitMessageLength" "src/Network" "initialization frame length validation is wired"
require_pattern "MaxMessageLength" "src/Network/MessageFrameValidator.cs" "message frames are bounded"
require_pattern "MaxInitMessageLength" "src/Network/MessageFrameValidator.cs" "initialization frames are bounded"
require_pattern "MessageFrameValidatorTests" "tests/Soulseek.Tests.Unit/Network/MessageFrameValidatorTests.cs" "frame validation regression tests are registered"
require_pattern "MaximumBufferedReadLength" "src/Network/Tcp/Connection.cs" "buffered reads have an allocation limit"

require_pattern "TrySet(Result|Exception|Canceled)" "src" "runtime task completion uses idempotent completion APIs"
require_absent_pattern "\.Set(Exception|Result|Canceled)\(" "src" "runtime source avoids non-idempotent task completion"
require_pattern "CreateLinkedTokenSource" "src/SoulseekClient.cs" "transfer races use linked cancellation"
require_pattern "Task\.WhenAny\\([\\s\\S]*disconnectedTaskCancellationSource\.Task" "src/SoulseekClient.cs" "transfer races include disconnect task"
require_pattern "RemoteTaskCompletionSource\.TrySetException" "src/SoulseekClient.cs" "remote transfer failures complete idempotently"

require_pattern "return false" "src/Ed25519PeerDescriptorSigner.cs" "peer descriptor verification fails closed"
require_pattern "catch[\\s\\S]*return false" "src/Ed25519PeerDescriptorSigner.cs" "peer descriptor verifier handles malformed signatures"
require_pattern "discovery hints rather than authorization decisions" "README.md" "peer capabilities are documented as non-authorization hints"

require_pattern "GetFullPathInsideRoot" "examples/Web/api/Extensions.cs" "example Web API has root containment helper"
require_pattern "GetSafeOutputPath" "examples/Web/api/Extensions.cs" "example Web API has safe output helper"
require_pattern "IsPathInsideRoot" "examples/Web/api/Extensions.cs" "example Web API checks normalized root containment"
require_pattern "WebApiPathSecurityTests" "tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs" "example path security tests are registered"

require_pattern "<PackageId>slskNet\.Runtime</PackageId>" "src/Soulseek.csproj" "package id uses fork branding"
require_pattern "snapetech/slskNet\.Runtime" "src/Soulseek.csproj" "package metadata points to fork repository"
require_pattern "^# slskNet\.Runtime" "README.md" "README uses runtime branding"
require_pattern "mcr\.microsoft\.com/dotnet/sdk:8\.0" ".circleci/config.yml" "CI uses current runtime SDK image"
require_absent_pattern "\"name\"\\s*:" "package.json" "repo root does not define Node package metadata"

require_pattern "bash scripts/check-remediation-baseline\.sh" "docs/dev/bug-burndown-ledger.md" "ledger references remediation baseline command"
require_pattern "RT-001" "docs/dev/bug-burndown-ledger.md" "ledger contains finding registry"

secret_pattern='-----BEGIN (RSA |DSA |EC |OPENSSH |PGP )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{36,}|xox[baprs]-[A-Za-z0-9-]{20,}|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)["'\'']?\s*[:=]\s*["'\''][A-Za-z0-9_./+=-]{24,}["'\'']'
require_absent_pattern "$secret_pattern" "." "tracked text files do not contain high-confidence secret patterns"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d remediation baseline check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll remediation baseline checks passed.\n'
