#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$repo_root/scripts/check-route-inventory.sh"
"$repo_root/scripts/check-controller-csrf.sh"
"/scripts/check-anonymous-endpoints.sh"
"/scripts/check-non-versioned-routes.sh"
"$repo_root/scripts/check-web-api-paths.sh"
"$repo_root/scripts/check-web-fetch-csrf.sh"
"$repo_root/scripts/check-web-mediacore-routes.sh"

"/scripts/check-remediation-script-registry.sh"

printf 'Remediation baseline checks passed.\n'
