#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

npm_commands="$(rg --no-filename -o 'npm run check:[A-Za-z0-9:-]+' "$repo_root/docs" | awk -F'npm run ' '{print $2}' | sort -u)"
while IFS= read -r command; do
  [ -n "$command" ] || continue
  if ! node -e "const pkg=require('./package.json'); process.exit(pkg.scripts && pkg.scripts[process.argv[1]] ? 0 : 1)" "$command" 2>/dev/null; then
    printf 'docs reference missing npm script: npm run %s\n' "$command" >&2
    failed=1
  fi
done <<< "$npm_commands"

script_refs="$(rg --no-filename -o 'scripts/check-[A-Za-z0-9_-]+\.sh' "$repo_root/docs" | sort -u)"
while IFS= read -r script; do
  [ -n "$script" ] || continue
  if [ ! -f "$repo_root/$script" ]; then
    printf 'docs reference missing remediation script: %s\n' "$script" >&2
    failed=1
  fi
done <<< "$script_refs"

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Remediation docs reference existing check commands.\n'
