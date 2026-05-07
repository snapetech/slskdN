#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
manifest="$repo_root/packaging/flatpak/io.github.slskd.slskdn.yml"
failed=0

if [ ! -f "$manifest" ]; then
  printf 'packaging/flatpak/io.github.slskd.slskdn.yml is missing\n' >&2
  exit 1
fi

ruby -e 'require "yaml"; YAML.load_file(ARGV.fetch(0))' "$manifest"

# Use grep instead of rg so this script runs on stock CI runners (GitHub Actions
# ubuntu-latest doesn't ship ripgrep).
if ! grep -qE '^        DEFAULT_CONFIG$' "$manifest"; then
  printf 'Flatpak wrapper default-config heredoc must close before manifest sources\n' >&2
  failed=1
fi

if ! grep -qE '^        EOF$' "$manifest"; then
  printf 'Flatpak wrapper heredoc must close before manifest sources\n' >&2
  failed=1
fi

if ! awk '
  /^  - name: slskdn$/ { in_module = 1; next }
  in_module && /^  - name: / { in_module = 0 }
  in_module && /^    sources:$/ { sources = NR }
  in_module && /^      - type: archive$/ { archive = NR }
  END { exit !(sources > 0 && archive > sources) }
' "$manifest"; then
  printf 'Flatpak slskdn archive source must be under the slskdn module sources block\n' >&2
  failed=1
fi

for asset in slskdn.desktop slskdn.metainfo.xml slskdn.svg; do
  if ! grep -qF "path: $asset" "$manifest"; then
    printf 'Flatpak manifest is missing source asset %s\n' "$asset" >&2
    failed=1
  fi
done

if awk '
  /^        cat > "\$SLSKD_CONFIG" << DEFAULT_CONFIG$/ { in_config = 1 }
  in_config && /^        DEFAULT_CONFIG$/ { in_config = 0 }
  in_config && /^      - type: archive$/ { bad = 1 }
  END { exit !bad }
' "$manifest"; then
  printf 'Flatpak release archive source is still embedded inside the wrapper config heredoc\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Flatpak manifest structure is valid.\n'
