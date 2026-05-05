#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
out="${1:-$repo_root/docs/system-surfaces-current.md}"
mapfile -t controllers < <(find "$repo_root/src/slskd" \( -path '*/API/*Controller.cs' -o -name '*Controller.cs' \) -type f | sort)

route_for() {
  local file="$1"
  rg -n '\[Route\(' "$file" | sed 's/.*\[Route(//; s/)\].*//' | tr '|' '/' | awk 'NR > 1 { printf "<br>" } { printf "%s", $0 }'
}

route_bucket() {
  local route="$1"
  case "$route" in
    *'api/v{version:apiVersion}'*|*'api/v0'*|*'api/v1'*) echo "versioned" ;;
    *'api/compatibility'*|'"api"'|'"api/'*) echo "legacy-or-compatibility" ;;
    *'"actors"'*|*'".well-known"'*) echo "federation-protocol" ;;
    *'"mesh/http"'*) echo "mesh-protocol" ;;
    *) echo "other" ;;
  esac
}

controller_count="${#controllers[@]}"
mutating_missing_csrf_count=0
anonymous_count=0
versioned_count=0
legacy_count=0
protocol_count=0
other_count=0

for f in "${controllers[@]}"; do
  if rg -q '\[Http(Post|Put|Delete|Patch)' "$f" && ! rg -q 'ValidateCsrfForCookiesOnly' "$f"; then
    mutating_missing_csrf_count=$((mutating_missing_csrf_count + 1))
  fi

  if rg -q '\[AllowAnonymous\]' "$f"; then
    anonymous_count=$((anonymous_count + 1))
  fi

  class_route="$(route_for "$f")"
  bucket="$(route_bucket "$class_route")"
  case "$bucket" in
    versioned) versioned_count=$((versioned_count + 1)) ;;
    legacy-or-compatibility) legacy_count=$((legacy_count + 1)) ;;
    federation-protocol|mesh-protocol) protocol_count=$((protocol_count + 1)) ;;
    *) other_count=$((other_count + 1)) ;;
  esac
done

{
  echo "# Current API surface inventory"
  echo
  echo "Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo
  echo "This inventory is generated from controller attributes. It is intended for parity/security review, not as a replacement for Swagger or integration tests."
  echo
  echo "## Summary"
  echo
  printf -- "- Controller files: %s\n" "$controller_count"
  printf -- "- Versioned API controllers: %s\n" "$versioned_count"
  printf -- "- Legacy or compatibility API controllers: %s\n" "$legacy_count"
  printf -- "- Protocol controllers outside versioned API routing: %s\n" "$protocol_count"
  printf -- "- Other route buckets: %s\n" "$other_count"
  printf -- "- Controllers with mutating HTTP methods and CSRF attribute missing: %s\n" "$mutating_missing_csrf_count"
  printf -- "- Controller files containing AllowAnonymous endpoints: %s\n" "$anonymous_count"
  echo
  echo "Route bucket policy: new web-consumed JSON APIs should be versioned. Non-versioned routes should be compatibility shims, protocol-required endpoints, or explicitly documented exceptions."
  echo
  echo "## Controllers"
  echo
  echo "| Controller | Class route | Bucket | Auth markers | CSRF | AllowAnonymous | HTTP actions |"
  echo "|---|---|---|---|---|---|---:|"

  for f in "${controllers[@]}"; do
    rel="${f#$repo_root/}"
    class_route="$(route_for "$f")"
    bucket="$(route_bucket "$class_route")"
    auth=$(rg -n '\[Authorize[^]]*\]' "$f" | sed 's/^[0-9]*://' | tr '\n' ' ' | sed 's/|/\\|/g; s/[[:space:]]\+/ /g; s/^ //; s/ $//')
    csrf="no"
    rg -q 'ValidateCsrfForCookiesOnly' "$f" && csrf="yes"
    anonymous="no"
    rg -q '\[AllowAnonymous\]' "$f" && anonymous="yes"
    actions=$(rg -n '\[Http(Get|Post|Put|Delete|Patch)' "$f" | wc -l | tr -d ' ')
    [ -n "$class_route" ] || class_route="(none found)"
    [ -n "$auth" ] || auth="(method-level or none)"
    printf '| `%s` | `%s` | %s | %s | %s | %s | %s |\n' "$rel" "$class_route" "$bucket" "$auth" "$csrf" "$anonymous" "$actions"
  done

  echo
  echo "## Non-versioned or protocol routes"
  echo
  echo "| Controller | Class route | Bucket | Review note |"
  echo "|---|---|---|---|"
  any_unversioned=false
  for f in "${controllers[@]}"; do
    rel="${f#$repo_root/}"
    class_route="$(route_for "$f")"
    bucket="$(route_bucket "$class_route")"
    if [ "$bucket" != "versioned" ]; then
      any_unversioned=true
      note="Review for versioned alias or documented exception."
      case "$bucket" in
        federation-protocol) note="Protocol-required ActivityPub/WebFinger route; keep outside /api/v0." ;;
        mesh-protocol) note="Mesh transport protocol route; keep outside public web API versioning." ;;
        legacy-or-compatibility) note="Legacy/compatibility route; prefer versioned alias for new UI clients." ;;
      esac
      printf '| `%s` | `%s` | %s | %s |\n' "$rel" "${class_route:-"(none found)"}" "$bucket" "$note"
    fi
  done
  if [ "$any_unversioned" = false ]; then
    echo "| none | - | - | - |"
  fi

  echo
  echo "## Mutating controllers missing CSRF"
  echo
  missing=false
  for f in "${controllers[@]}"; do
    if rg -q '\[Http(Post|Put|Delete|Patch)' "$f" && ! rg -q 'ValidateCsrfForCookiesOnly' "$f"; then
      missing=true
      echo "- ${f#$repo_root/}"
    fi
  done
  if [ "$missing" = false ]; then
    echo
    echo "None found."
  fi

  echo
  echo "## Controllers with anonymous endpoints"
  echo
  any=false
  for f in "${controllers[@]}"; do
    if rg -q '\[AllowAnonymous\]' "$f"; then
      any=true
      echo "- ${f#$repo_root/}"
      rg -n '\[AllowAnonymous\]|\[Http(Get|Post|Put|Delete|Patch)' "$f" | sed 's/^/  - /'
    fi
  done
  if [ "$any" = false ]; then
    echo
    echo "None found."
  fi
} > "$out"

echo "Wrote $out"
