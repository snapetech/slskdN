#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${GPG_PRIVATE_KEY:-}" ]]; then
  echo "GPG_PRIVATE_KEY secret not set. Skipping PPA upload."
  printf 'fingerprint=\n' >> "${GITHUB_OUTPUT:?GITHUB_OUTPUT is required by the PPA workflow}"
  exit 0
fi

if [[ -z "${GITHUB_OUTPUT:-}" ]]; then
  echo "GITHUB_OUTPUT is required to publish the imported signing fingerprint." >&2
  exit 1
fi

printf '%s\n' "$GPG_PRIVATE_KEY" | gpg --batch --import

fingerprint="$(gpg --batch --with-colons --list-secret-keys | awk -F: '
  $1 == "sec" { primary = 1; next }
  primary && $1 == "fpr" { print $10; exit }
')"

if [[ -z "$fingerprint" ]]; then
  echo "::error::GPG_PRIVATE_KEY imported no usable secret signing key."
  exit 1
fi

temporary_directory="$(mktemp -d)"
trap 'rm -rf "$temporary_directory"' EXIT
printf 'slskdn PPA signing preflight\n' > "$temporary_directory/payload"

if ! gpg --batch --yes --local-user "$fingerprint" \
  --output "$temporary_directory/payload.sig" --detach-sign "$temporary_directory/payload"; then
  echo "::error::The imported PPA key cannot sign in unattended batch mode."
  exit 1
fi

if ! gpg --batch --verify "$temporary_directory/payload.sig" "$temporary_directory/payload" >/dev/null 2>&1; then
  echo "::error::The imported PPA signing key failed its signature preflight."
  exit 1
fi

printf 'fingerprint=%s\n' "$fingerprint" >> "$GITHUB_OUTPUT"
printf 'PPA signing key verified: %s\n' "${fingerprint: -16}"
