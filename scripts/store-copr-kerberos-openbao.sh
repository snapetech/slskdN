#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat >&2 <<'EOF'
usage: scripts/store-copr-kerberos-openbao.sh --principal <principal> --keytab <path> [--secret-path <path>]

Stores Copr Kerberos credentials in OpenBao without printing the keytab bytes.
Requires an authenticated bao CLI session.
EOF
}

repo_name="$(basename "$(git rev-parse --show-toplevel 2>/dev/null || pwd)" | tr '[:upper:]' '[:lower:]')"
secret_path="secret/${repo_name}/release-publishing"
principal=""
keytab=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --principal)
      principal="${2:-}"
      shift 2
      ;;
    --keytab)
      keytab="${2:-}"
      shift 2
      ;;
    --secret-path)
      secret_path="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      usage
      exit 2
      ;;
  esac
done

if ! command -v bao >/dev/null 2>&1; then
  echo "bao CLI is not installed" >&2
  exit 1
fi

if [[ -z "$principal" || -z "$keytab" ]]; then
  usage
  exit 2
fi

if [[ ! "$principal" =~ @FEDORAPROJECT\.ORG$ ]]; then
  echo "principal must include the FEDORAPROJECT.ORG realm, for example user@FEDORAPROJECT.ORG" >&2
  exit 2
fi

if [[ ! -f "$keytab" ]]; then
  echo "keytab not found: $keytab" >&2
  exit 1
fi

if [[ ! -r "$keytab" ]]; then
  echo "keytab is not readable: $keytab" >&2
  exit 1
fi

bao kv patch "$secret_path" \
  copr_kerberos_principal="$principal" \
  copr_kerberos_keytab_b64="$(base64 -w0 "$keytab")"

echo "stored Copr Kerberos credentials in $secret_path"
