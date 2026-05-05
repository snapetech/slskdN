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

expect_regex() {
  local file="$1"
  local pattern="$2"

  if ! grep -Eq -- "$pattern" "$repo_root/$file"; then
    printf '%s is missing pattern: %s\n' "$file" "$pattern" >&2
    failed=1
  fi
}

expect_regex packaging/aur/slskd.service '^User=slskd$'
expect_regex packaging/aur/slskd.service '^Group=slskd$'
expect_regex packaging/aur/slskd.service '^UMask=0002$'
expect_regex packaging/aur/slskd.sysusers '^u slskd '
expect_regex packaging/aur/slskd.tmpfiles '^d /var/lib/slskd 0775 slskd slskd -$'
expect_regex packaging/aur/slskd.tmpfiles '^d /var/lib/slskd/downloads 0775 slskd slskd -$'
expect_regex packaging/aur/slskd.tmpfiles '^d /var/lib/slskd/incomplete 0775 slskd slskd -$'
expect_regex packaging/aur/slskd.tmpfiles '^z /etc/slskd/slskd\.yml 0664 slskd slskd -$'
expect_literal packaging/aur/slskd.install 'systemd-sysusers'
expect_literal packaging/aur/slskd.install 'systemd-tmpfiles --create /usr/lib/tmpfiles.d/slskd.conf'
expect_literal packaging/linux/install-from-release.sh 'UMask=0002'
expect_literal packaging/linux/install-from-release.sh 'chown "${USER}:${USER}" "$CONFIG_FILE"'
expect_literal packaging/linux/install-from-release.sh 'chmod 664 "$CONFIG_FILE"'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Linux packages must converge daemon user, config/data ownership, group-write mode, and systemd umask together.
MSG
  exit 1
fi

printf 'Systemd package permission matrix is complete.\n'
