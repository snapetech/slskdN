#!/usr/bin/env bash
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

write_section \
  "Event-style async boundaries" \
  'async void' \
  src examples/Web/api

write_section \
  "Silent catch or lossy exception boundaries" \
  'catch \(Exception\)(?:\s*when[^{]+)?\s*\{\s*(?://\s*(?:noop|ignored?)\s*)?\s*\}' \
  src examples/Web/api

write_section \
  "Callback/event invocation boundaries" \
  '\?\.(Invoke|BeginInvoke)\(|\.Invoke\(' \
  src examples/Web/api

write_section \
  "Unisolated server handler event invocations" \
  '^(?![[:space:]]*\(\) =>)(?!.*RaiseEventHandler)(?!.*DiagnosticGenerated).*[A-Za-z0-9_]+\?\.Invoke\(' \
  src/Messaging/Handlers/ServerMessageHandler.cs

{
  printf '\n## Unisolated message connection event invocations\n'
  awk '
    /private void RaiseEventHandler/ { exit }
    /\.Invoke\(/ { printf "%s:%d:%s\n", FILENAME, NR, $0 }
  ' src/Network/MessageConnection.cs
} >>"$report"

{
  printf '\n## Unisolated TCP connection event invocations\n'
  awk '
    /private void RaiseEventHandler/ { exit }
    /\.Invoke\(this,/ { printf "%s:%d:%s\n", FILENAME, NR, $0 }
  ' src/Network/Tcp/Connection.cs
} >>"$report"

{
  printf '\n## Unisolated client lifecycle event invocations\n'
  rg -n --with-filename --pcre2 \
    '^(?!.*Raise(EventHandler|StateChanged|Connected|LoggedIn|Disconnected|ServerInfoReceived)).*(?<![A-Za-z0-9_])(StateChanged|Connected|LoggedIn|Disconnected|ServerInfoReceived)\?\.Invoke\(this,' \
    src/SoulseekClient.cs || true
} >>"$report"

{
  printf '\n## Unisolated client search event invocations\n'
  rg -n --with-filename --pcre2 \
    '^(?!.*Raise(SearchStateChanged|SearchResponseReceived|EventHandler)).*(SearchStateChanged|SearchResponseReceived)\?\.Invoke\(this,' \
    src/SoulseekClient.cs || true
} >>"$report"

{
  printf '\n## Unisolated client transfer/browse event invocations\n'
  rg -n --with-filename --pcre2 \
    '^(?!.*Raise(BrowseProgressUpdated|Transfer(StateChanged|ProgressUpdated)|EventHandler)).*(BrowseProgressUpdated|Transfer(StateChanged|ProgressUpdated))\?\.Invoke\(this,' \
    src/SoulseekClient.cs || true
} >>"$report"

{
  printf '\n## Unisolated SoulseekClient bridge event invocations\n'
  rg -n --with-filename --pcre2 \
    '^[[:space:]]*[A-Za-z0-9_.]+\.[A-Za-z0-9_]+ \+= \(sender, e\) => (?!Raise(DiagnosticGenerated|EventHandler)).*\?\.Invoke\(' \
    src/SoulseekClient.cs || true
} >>"$report"

write_section \
  "Remote/user text in diagnostics or HTTP errors" \
  '(Diagnostic\.(Debug|Info|Warning|Error)|StatusCode\(|BadRequest\(|Console\.WriteLine)\([^;\n]*(username|query|filename|directory|token|Message)' \
  src examples/Web/api

write_section \
  "Public mutable ownership surfaces" \
  'public [^;\n=]*\[\][^{;\n]*(\{|=>|;)|public .*I(ReadOnly)?(Collection|List|Enumerable)<|params ' \
  src examples/Web/api

printf 'Active council bughunt candidates saved to %s.\n' "$report"
printf 'Verdict boundary: this report is a discovery queue, not proof of no bugs.\n'
