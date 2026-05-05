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

expect_literal src/web/src/lib/chat.js 'JSON.stringify(message)'
expect_literal src/web/src/lib/rooms.js 'JSON.stringify(roomName)'
expect_literal src/web/src/lib/rooms.js 'JSON.stringify(message)'
expect_literal src/web/src/lib/options.js 'JSON.stringify(yaml)'
expect_literal src/web/src/lib/mediacore.js 'JSON.stringify(contentId)'
expect_literal src/web/src/lib/server.js 'data: JSON.stringify(message)'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

ASP.NET [FromBody] string actions expect a JSON string literal when the Web API client sends application/json.
Wrap primitive string payloads with JSON.stringify(value), or use an object body when the controller expects a DTO.
MSG
  exit 1
fi

printf 'Known primitive Web JSON string bodies are explicitly serialized.\n'
