#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

expect_literal() {
  local file="$1"
  local literal="$2"

  if ! grep -Fq -- "$literal" "$repo_root/$file"; then
    printf '%s is missing literal: %s\n' "$file" "$literal" >&2
    failed=1
  fi
}

for installer in \
  packaging/linux/install-from-release.sh \
  packaging/proxmox-lxc/setup-inside-ct.sh; do
  expect_literal "$installer" 'SHA256SUMS.txt'
  expect_literal "$installer" 'verify_asset_checksum'
  expect_literal "$installer" 'rm -rf "$DEST"'
  expect_literal "$installer" 'chown -R "${USER}:${USER}" "$DATA_DIR" "$DEST"'
  expect_literal "$installer" 'chmod -R g+rwX "$DATA_DIR"'
  expect_literal "$installer" 'chmod 664 "$CONFIG_FILE"'
  expect_literal "$installer" 'UMask=0002'
done

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Linux release installers must verify release checksums, replace stale install
trees, and keep service ownership/mode posture aligned across raw and Proxmox
install paths.
MSG
  exit 1
fi

printf 'Linux release installers verify assets and converge service permissions.\n'
