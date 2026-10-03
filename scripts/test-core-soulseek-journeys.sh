#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

image='ghcr.io/soulfind-dev/soulfind@sha256:82e3a88bd4c48ddf149bd2c3485c1586af5044e28a67969b934a29e34aca6228'
for executable in docker node pnpm python3; do
  if ! command -v "$executable" >/dev/null 2>&1; then
    echo "Required executable is unavailable: $executable" >&2
    exit 1
  fi
done

node --disable-warning=MODULE_TYPELESS_PACKAGE_JSON --experimental-strip-types \
  --test src/web/e2e/harness/soulseekPrivateMessageIdMapper.node-test.mjs

if ! docker image inspect "$image" >/dev/null 2>&1; then
  echo "The pinned loopback test image is unavailable: $image" >&2
  exit 1
fi

container_name="slskdn-core-journeys-$$-${RANDOM}"
container_started=0
cleanup() {
  if [[ "$container_started" == 1 ]]; then
    docker rm --force "$container_name" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

docker run --pull=never --detach --name "$container_name" \
  --label slskdn.test=core-soulseek-journeys \
  --publish '127.0.0.1::2242/tcp' "$image" >/dev/null
container_started=1

host_port=''
for _ in $(seq 1 60); do
  binding="$(docker port "$container_name" 2242/tcp 2>/dev/null | head -n 1 || true)"
  host_port="${binding##*:}"
  if [[ "$host_port" =~ ^[0-9]+$ ]] && python3 - "$host_port" <<'PY'
import socket
import sys

with socket.create_connection(("127.0.0.1", int(sys.argv[1])), timeout=0.5):
    pass
PY
  then
    break
  fi
  host_port=''
  sleep 1
done

if [[ -z "$host_port" ]]; then
  echo 'The loopback Soulfind test service did not become ready within 60 seconds.' >&2
  docker logs "$container_name" >&2 || true
  exit 1
fi

echo "Using loopback Soulfind on 127.0.0.1:$host_port; the test nodes do not contact public peers."
pnpm --filter @slskdn/web run build
RUN_CORE_SOULSEEK_JOURNEYS=1 \
SLSKDN_E2E_SOULSEEK_ADDRESS=127.0.0.1 \
SLSKDN_E2E_SOULSEEK_PORT="$host_port" \
SLSKD_UPLOAD_SPEED_LIMIT=128 \
SLSKD_DOWNLOAD_SPEED_LIMIT=128 \
pnpm --filter @slskdn/web exec playwright test \
  e2e/core-soulseek-journeys.spec.ts \
  --workers=1 --retries=0 --trace=retain-on-failure --reporter=line
