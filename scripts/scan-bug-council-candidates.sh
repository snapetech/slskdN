#!/usr/bin/env bash
#
# Bug Council candidate scanner — TEMPLATE.
#
# Copy into your-repo/scripts/scan-bug-council-candidates.sh and replace the
# scan() invocations with the patterns that fit your codebase. Examples for
# C#, Rust, TypeScript/JavaScript, Go, and Python are included below; comment
# in the ones that apply and delete the rest.
#
# Output is intentionally noisy: it is the durable discovery queue, not the
# pass/fail gate. The pass/fail gate is scripts/check-remediation-baseline.sh.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

scan() {
  local title="$1"
  local pattern="$2"
  shift 2

  printf '\n## %s\n' "$title"
  rg -n --with-filename --pcre2 --hidden --glob '!.git/**' "$pattern" "$@" || true
}

printf '# Council candidate scan\n'
printf '# Generated: %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')"

# === Universal patterns ====================================================

scan "Security-sensitive material candidates" \
  'PRIVATE KEY|gh[pousr]_[A-Za-z0-9_]{36,}|xox[baprs]-[A-Za-z0-9-]{20,}|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)' \
  .

# === C# / .NET examples ====================================================
#
# scan "Mutable public byte arrays and array properties" \
#   'public [^;\n=]*\[\][^{;\n]*(\{|=>|;)|\bbyte\[\]\s+[A-Z][A-Za-z0-9_]*\s*\{' \
#   src tests
#
# scan "Non-idempotent task completion candidates" \
#   '\.Set(Result|Exception|Canceled)\(' \
#   src
#
# scan "Protocol count and length allocation candidates" \
#   'ReadInteger\(\)|ReadLong\(\)|ReadBytes\([^)]*\)|new byte\[[^]]+\]' \
#   src

# === Rust examples =========================================================
#
# scan "Protocol count/length candidates (Rust)" \
#   'read_u32_le\(\)\? as usize|Vec::with_capacity\(|read_bytes\(length\)' \
#   crates
#
# scan "Task / cancellation lifecycle candidates (Rust)" \
#   'tokio::spawn|select!|timeout\(|sleep\(|interval\(|mpsc|broadcast|oneshot' \
#   crates

# === TypeScript / JavaScript examples ======================================
#
# scan "DOM/HTML injection candidates (TS/JS)" \
#   'innerHTML\s*=|dangerouslySetInnerHTML|document\.write\(|eval\(|new Function\(' \
#   src
#
# scan "Auth/storage/CORS leakage candidates (TS/JS)" \
#   'localStorage|window\.open|target="_blank"|Authorization: Bearer|Access-Control-Allow-Origin: \*' \
#   src

# === Go examples ===========================================================
#
# scan "Goroutine and context lifecycle candidates (Go)" \
#   'go func|context\.Background\(\)|context\.TODO\(\)|<-time\.After|select \{' \
#   .

# === Python examples =======================================================
#
# scan "Constructor mutable input candidates (Python)" \
#   'def __init__\([^)]*(list|dict|List|Dict)\[' \
#   .

printf '\n# End of candidate scan. Every hit must be ledgered as Fixed, Existing guard, False positive, or Out of scope before a council sweep is closed.\n'
