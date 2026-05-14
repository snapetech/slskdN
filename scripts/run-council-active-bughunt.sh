#!/usr/bin/env bash
#
# Bug Council active bughunt runner — TEMPLATE.
#
# This runner is a discovery queue, not a pass/fail gate. Keep it wired into
# run-bug-council-all-phases.sh so every council cycle refreshes suspicious
# shapes that sit outside the closed sweep registers.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out_dir="${COUNCIL_OUT_DIR:-.council}"
mkdir -p "$out_dir"
report="$out_dir/active-bughunt.md"

write_section() {
  local title="$1"
  local pattern="$2"
  shift 2

  {
    printf '\n## %s\n' "$title"
    rg -n -U --with-filename --pcre2 --hidden --glob '!.git/**' --glob '!.council/**' "$pattern" "$@" || true
  } >>"$report"
}

cat >"$report" <<'EOF'
# Active Council Bughunt Candidate Report

This report is not a pass/fail proof. It is a fresh queue of suspicious shapes
that sit outside, or at the edge of, the current closed sweep gates. A green
all-phases council run means registered gates passed; it does not mean these
candidate lines are bugs or that no bugs exist.

Classification rule: any accepted row must be ledgered, fixed with behavior
coverage, sibling-swept, and promoted into a durable gate before closure.
EOF

# Replace paths and patterns for your repo. Add narrow sections whenever a
# confirmed bug class is fixed, so the next run proves that specific shape is
# gone while the broader scanner remains free to be noisy.
write_section \
  "Async void boundaries" \
  'async void' \
  src tests examples

write_section \
  "Silent catch or lossy exception boundaries" \
  'catch \(Exception\)(?:\s*when[^{]+)?\s*\{\s*(?://\s*(?:noop|ignored?)\s*)?\s*\}' \
  src tests examples

write_section \
  "Callback/event invocation boundaries" \
  '\?\.(Invoke|BeginInvoke)\(|\.Invoke\(' \
  src tests examples

write_section \
  "Remote/user text in diagnostics or HTTP errors" \
  '(log|logger|Diagnostic|Console\.WriteLine|StatusCode\(|BadRequest\()[^;\n]*(username|query|filename|directory|token|message)' \
  src tests examples

write_section \
  "Red-team abuse lens" \
  '(token|secret|password|authorization|cookie|api[-_]?key|session|redirect|proxy|forwarded|path|filename|exec|spawn|shell|http://|https://)' \
  src tests examples scripts docs

write_section \
  "Public mutable ownership surfaces" \
  'public [^;\n=]*\[\][^{;\n]*(\{|=>|;)|public .*I(ReadOnly)?(Collection|List|Enumerable)<|params ' \
  src tests examples

printf 'Active council bughunt candidates saved to %s.\n' "$report"
printf 'Verdict boundary: this report is a discovery queue, not proof of no bugs.\n'
