#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

scan() {
  local title="$1"
  local pattern="$2"
  shift 2

  printf '\n## %s\n' "$title"
  rg -n --pcre2 --hidden --glob '!.git/**' "$pattern" "$@" || true
}

printf '# slskNet.Runtime bug council candidate scan\n'
printf '# Generated: %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')"

scan "Mutable public byte arrays and array properties" \
  'public [^;\n=]*\[\][^{;\n]*(\{|=>|;)|\bbyte\[\]\s+[A-Z][A-Za-z0-9_]*\s*\{' \
  src tests/Soulseek.Tests.Unit

scan "Constructors accepting mutable collections or params arrays" \
  '\bpublic [A-Za-z0-9_]+\([^)]*(IEnumerable<|IReadOnlyCollection<|ICollection<|IList<|List<|Dictionary<|HashSet<|params )' \
  src

scan "Value equality and hash-code comparisons" \
  'GetHashCode\(\)\s*==|Equals\([^)]*GetHashCode|operator ==|operator !=|public bool Equals\(' \
  src

scan "Non-idempotent task completion candidates" \
  '\.Set(Result|Exception|Canceled)\(' \
  src

scan "Task, cancellation, timer, and semaphore lifecycle candidates" \
  'TaskCompletionSource|CancellationTokenSource|SemaphoreSlim|Timer|Task\.WhenAny|ContinueWith|async void|Register\(' \
  src

scan "Protocol count and length allocation candidates" \
  'ReadInteger\(\)|ReadLong\(\)|ReadBytes\([^)]*\)|new byte\[[^]]+\]|for \(int i = 0; i < [^;]+; i\+\+\)|while \(' \
  src/Messaging src/Network

scan "Protocol scalar emission candidates" \
  'Write(Integer|Long|Byte|String|Bytes)\(' \
  src/Messaging

scan "Resolver output and raw stream candidates" \
  'Resolver|Raw.*Response|Stream|WriteAsync\([^)]*Stream|ToByteArray\(\)' \
  src/Messaging src/Options src/SoulseekClient.cs

scan "Example Web API path, request, and lifecycle candidates" \
  'Path|File\.|Directory\.|Request|CancellationTokenSource|Stream|IFormFile|FromBody|ActionResult|BadRequest|Ok\(' \
  examples/Web/api tests/Soulseek.Tests.Unit/WebApi*

scan "Security-sensitive material candidates" \
  'PRIVATE KEY|gh[pousr]_|xox[baprs]-|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)' \
  .

printf '\n# End of candidate scan. Every hit must be ledgered as Fixed, Existing guard, False positive, or Out of scope before a council sweep is closed.\n'
