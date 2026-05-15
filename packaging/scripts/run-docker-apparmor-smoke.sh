#!/usr/bin/env bash

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

IMAGE="${SLSKDN_DOCKER_IMAGE:-slskdn:apparmor-perms-test2}"
PROFILE="${SLSKDN_APPARMOR_PROFILE:-packaging/docker/apparmor/slskdn-docker}"
CONTAINER="${SLSKDN_APPARMOR_CONTAINER:-slskdn-apparmor-smoke}"

fail() {
  echo "ERROR: $1" >&2
  exit 1
}

skip() {
  echo "SKIP: $1" >&2
  exit 2
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "$1 is required"
}

cleanup() {
  docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
  for dir in "${APP_DIR:-}" "${DOWNLOADS_DIR:-}" "${SHARES_DIR:-}" "${CHROME_PROFILE:-}"; do
    if [ -n "$dir" ] && [ -d "$dir" ]; then
      chmod -R u+rwX "$dir" 2>/dev/null || true
      rm -rf "$dir" 2>/dev/null || true
    fi
  done
}

trap cleanup EXIT

require_cmd docker
require_cmd curl
require_cmd apparmor_parser

[ -f "$PROFILE" ] || fail "AppArmor profile not found: $PROFILE"

if [ "$(cat /sys/module/apparmor/parameters/enabled 2>/dev/null || true)" != "Y" ]; then
  skip "kernel AppArmor support is not enabled"
fi

if ! docker info --format '{{json .SecurityOptions}}' | grep -q 'apparmor'; then
  skip "Docker daemon is not advertising AppArmor support"
fi

apparmor_parser -Q -T "$PROFILE"

if [ "$(id -u)" -eq 0 ]; then
  apparmor_parser -r "$PROFILE"
else
  require_cmd sudo
  sudo apparmor_parser -r "$PROFILE"
fi

APP_DIR="$(mktemp -d)"
DOWNLOADS_DIR="$(mktemp -d)"
SHARES_DIR="$(mktemp -d)"
chmod 0775 "$APP_DIR" "$DOWNLOADS_DIR" "$SHARES_DIR"

docker run -d --name "$CONTAINER" \
  -p 127.0.0.1::5030 \
  --user "$(id -u):$(id -g)" \
  --read-only \
  --tmpfs /tmp:rw,noexec,nosuid,nodev,size=256m \
  --tmpfs /run:rw,noexec,nosuid,nodev,size=32m,mode=1777 \
  --cap-drop ALL \
  --security-opt no-new-privileges:true \
  --security-opt apparmor=slskdn-docker \
  --pids-limit 512 \
  --memory 2g \
  --memory-swap 2g \
  -e SLSKD_APP_DIR=/app \
  -e SLSKD_HTTP_ADDRESS=0.0.0.0 \
  -e SLSKD_HTTP_PORT=5030 \
  -e SLSKD_WEBUI_HTTPS=false \
  -e SLSKD_NO_HTTPS=true \
  -e SLSKD_REMOTE_CONFIGURATION=true \
  -e SLSKD_UMASK=0007 \
  -e SLSKD_STRICT_APP_DIR_PERMISSIONS=true \
  -e DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/.net \
  -v "$APP_DIR:/app" \
  -v "$DOWNLOADS_DIR:/downloads" \
  -v "$SHARES_DIR:/shares:ro" \
  "$IMAGE" >/dev/null

PORT="$(docker port "$CONTAINER" 5030/tcp | sed 's/.*://')"
[ -n "$PORT" ] || fail "could not determine published HTTP port"

for _ in $(seq 1 60); do
  code="$(curl -fsS -o /tmp/slskdn-apparmor-health.out -w '%{http_code}' "http://127.0.0.1:${PORT}/health" 2>/dev/null || true)"
  [ "$code" = 200 ] && break
  sleep 2
done

[ "${code:-}" = 200 ] || {
  docker logs "$CONTAINER" >&2 || true
  fail "health endpoint did not return HTTP 200"
}

curl -fsS -o /tmp/slskdn-apparmor-root.html "http://127.0.0.1:${PORT}/"
api_code="$(curl -sS -o /tmp/slskdn-apparmor-app.json -w '%{http_code}' "http://127.0.0.1:${PORT}/api/v0/application")"
[ "$api_code" = 401 ] || fail "protected API expected HTTP 401, got $api_code"

docker exec "$CONTAINER" sh -c '
  test "$(stat -c %a /app)" = "770"
  test "$(awk "/^CapEff:/ { print \$2 }" /proc/1/status)" = "0000000000000000"
  test "$(awk "/^NoNewPrivs:/ { print \$2 }" /proc/1/status)" = "1"
  test "$(awk "/^Seccomp:/ { print \$2 }" /proc/1/status)" = "2"
  touch /app/write-test
  touch /downloads/write-test
  ! touch /shares/write-test 2>/tmp/share-write-error
'

if command -v chromium >/dev/null 2>&1; then
  CHROME_PROFILE="$(mktemp -d)"
  chromium --headless --no-sandbox --disable-gpu \
    --user-data-dir="$CHROME_PROFILE" \
    --virtual-time-budget=8000 \
    --dump-dom "http://127.0.0.1:${PORT}/" >/tmp/slskdn-apparmor-dom.html
  grep -Eq 'id="root"|slskd|slskdN' /tmp/slskdn-apparmor-dom.html || fail "headless Chromium did not load expected Web UI DOM"
fi

if docker logs "$CONTAINER" 2>&1 | grep -Eiq 'err|fatal|exception|permission denied|read-only file system'; then
  docker logs "$CONTAINER" >&2
  fail "container logs contain error-like signatures"
fi

echo "Docker AppArmor smoke passed for $IMAGE on http://127.0.0.1:${PORT}"
