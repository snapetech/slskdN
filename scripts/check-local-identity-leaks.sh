#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

tmp_tokens="$(mktemp)"
tmp_unreleased="$(mktemp)"
tmp_commits="$(mktemp)"
trap 'rm -f "$tmp_tokens" "$tmp_unreleased" "$tmp_commits"' EXIT

add_token() {
  local token="$1"
  token="${token//$'\n'/}"
  token="${token//$'\r'/}"
  [[ ${#token} -ge 3 ]] || return 0
  case "$token" in
    root|runner|build|agent|slskd|slskdn|snapetech)
      return 0
      ;;
  esac
  printf '%s\n' "$token" >>"$tmp_tokens"
}

add_token "${SLSKDN_FORBIDDEN_LOCAL_HOSTNAME:-}"
add_token "$(hostname -s 2>/dev/null || true)"
add_token "${USER:-}"
add_token "$(id -un 2>/dev/null || true)"
add_token "$(basename "${HOME:-}" 2>/dev/null || true)"

if [[ -n "${SLSKDN_LOCAL_IDENTITY_DENYLIST:-}" ]]; then
  IFS=',' read -ra denylist_tokens <<<"$SLSKDN_LOCAL_IDENTITY_DENYLIST"
  for token in "${denylist_tokens[@]}"; do
    add_token "$token"
  done
fi

if [[ -n "${SLSKDN_LOCAL_IDENTITY_DENYLIST_FILE:-}" && -f "$SLSKDN_LOCAL_IDENTITY_DENYLIST_FILE" ]]; then
  while IFS= read -r token; do
    [[ "$token" =~ ^[[:space:]]*# ]] && continue
    add_token "$token"
  done <"$SLSKDN_LOCAL_IDENTITY_DENYLIST_FILE"
fi

sort -u "$tmp_tokens" -o "$tmp_tokens"

awk '
  $0 == "## [Unreleased]" { in_section = 1; next }
  in_section && /^## \[/ { exit }
  in_section { print }
' docs/CHANGELOG.md >"$tmp_unreleased"

latest_release_tag="$(
  git tag --sort=-creatordate --list 'build-main-*' | head -n 1 || true
)"
if [[ -n "$latest_release_tag" ]]; then
  git log --format='%s%n%b' "${latest_release_tag}..HEAD" >"$tmp_commits"
else
  git log --format='%s%n%b' -n 50 HEAD >"$tmp_commits"
fi

failed=0

check_stream() {
  local label="$1"
  local path="$2"

  if rg -n --fixed-strings --ignore-case --file "$tmp_tokens" "$path"; then
    printf 'Local hostname/username leaked into %s. Use generic wording like "live validation host" or "operator account".\n' "$label" >&2
    failed=1
  fi
}

check_stream "docs/CHANGELOG.md Unreleased" "$tmp_unreleased"
check_stream "recent commit messages" "$tmp_commits"

for path in \
  .github/release-notes/main.md.tmpl \
  docs/dev/release-copy.md \
  packaging/winget/snapetech.slskdn.locale.en-US.yaml
do
  [[ -f "$path" ]] || continue
  check_stream "$path" "$path"
done

if [[ "$failed" -ne 0 ]]; then
  exit 1
fi

printf 'No local hostname or username leaks found in release-facing text.\n'
