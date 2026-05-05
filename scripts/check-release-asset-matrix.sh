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

expect_literal .github/workflows/build-on-tag.yml 'slskdn-main-linux-glibc-x64.zip'
expect_literal .github/workflows/build-on-tag.yml 'slskdn-main-win-x64.zip'
expect_literal .github/workflows/build-on-tag.yml 'slskd.tmpfiles'
expect_literal packaging/aur/PKGBUILD-bin 'slskdn-${pkgver}-main-linux-glibc-x64.zip'
expect_literal packaging/homebrew/Formula/slskdn.rb 'slskdn-main-linux-glibc-x64.zip'
expect_literal packaging/chocolatey/tools/chocolateyinstall.ps1 'slskdn-main-win-x64.zip'
expect_literal packaging/winget/snapetech.slskdn.installer.yaml 'slskdn-main-win-x64.zip'
expect_literal packaging/rpm/slskdn.spec 'slskd.tmpfiles'
expect_literal .github/release-notes/main.md.tmpl 'slskdn-main-linux-glibc-x64.zip'
expect_literal .github/release-notes/main.md.tmpl 'slskdn-main-linux-glibc-arm64.zip'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Release workflow asset names must stay aligned with package-manager metadata.
Update this matrix with any intentional asset rename.
MSG
  exit 1
fi

printf 'Release asset matrix matches package metadata expectations.\n'
