#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temporary_directory="$(mktemp -d)"
trap 'rm -rf "$temporary_directory"' EXIT

source_home="$temporary_directory/source-gnupg"
import_home="$temporary_directory/import-gnupg"
mkdir -m 700 "$source_home" "$import_home"

GNUPGHOME="$source_home" gpg --batch --pinentry-mode loopback --passphrase '' \
  --quick-generate-key 'slskdn PPA signing test <ppa-signing-test@example.invalid>' ed25519 sign 0 \
  >/dev/null 2>&1

expected_fingerprint="$(GNUPGHOME="$source_home" gpg --batch --with-colons --list-secret-keys | awk -F: '
  $1 == "sec" { primary = 1; next }
  primary && $1 == "fpr" { print $10; exit }
')"
private_key="$(GNUPGHOME="$source_home" gpg --batch --armor --export-secret-keys "$expected_fingerprint")"

output_file="$temporary_directory/github-output"
: > "$output_file"
GNUPGHOME="$import_home" GPG_PRIVATE_KEY="$private_key" GITHUB_OUTPUT="$output_file" \
  "$repo_root/scripts/import-ppa-signing-key.sh"

actual_fingerprint="$(sed -n 's/^fingerprint=//p' "$output_file")"
if [[ "$actual_fingerprint" != "$expected_fingerprint" ]]; then
  echo "The PPA import helper returned a different signing fingerprint." >&2
  exit 1
fi

missing_key_home="$temporary_directory/missing-key-gnupg"
mkdir -m 700 "$missing_key_home"
missing_key_output="$temporary_directory/missing-key-output"
: > "$missing_key_output"
GNUPGHOME="$missing_key_home" GITHUB_OUTPUT="$missing_key_output" \
  "$repo_root/scripts/import-ppa-signing-key.sh"
if ! grep -qx 'fingerprint=' "$missing_key_output"; then
  echo "The missing-secret path did not return an empty signing fingerprint." >&2
  exit 1
fi

expected_debuild_command="debuild -S -sa -d -k\"\$GPG_SIGNING_FINGERPRINT\""
for workflow in .github/workflows/release-ppa.yml .github/workflows/build-on-tag.yml; do
  if ! grep -Fq 'run: scripts/import-ppa-signing-key.sh' "$repo_root/$workflow"; then
    echo "$workflow does not use the shared PPA signing-key helper." >&2
    exit 1
  fi
  if ! grep -Fq "$expected_debuild_command" "$repo_root/$workflow"; then
    echo "$workflow does not sign its source package with the imported fingerprint." >&2
    exit 1
  fi
  if ! grep -Fq "if: steps.ppa-signing-key.outputs.fingerprint != ''" "$repo_root/$workflow"; then
    echo "$workflow does not skip source builds when the signing key is absent." >&2
    exit 1
  fi
done

printf 'PPA signing-key import and unattended signature preflight passed.\n'
