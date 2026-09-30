#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

image='slskdn-player-a11y:orca-clean'
for executable in dbus-run-session docker pactl parec pnpm python3 xvfb-run; do
  if ! command -v "$executable" >/dev/null 2>&1; then
    echo "Required executable is unavailable: $executable" >&2
    exit 1
  fi
done

if ! docker image inspect "$image" >/dev/null 2>&1; then
  docker build --tag "$image" --file scripts/player-a11y/Dockerfile scripts/player-a11y
fi

evidence_directory="$repo_root/.local/player-a11y-evidence"
mkdir -p "$evidence_directory"
run_id="$(date -u +%Y%m%dT%H%M%SZ)-$$"
runtime_directory="$evidence_directory/runtime-$run_id"
home_directory="$evidence_directory/home-$run_id"
cache_directory="$evidence_directory/cache-$run_id"
debug_log="$evidence_directory/orca-$run_id.log"
applications_log="$evidence_directory/orca-applications-$run_id.txt"
pcm_capture="$evidence_directory/orca-speech-$run_id.s16le"
sink_name="slskdn_player_a11y_$$"
container_name="slskdn-player-a11y-$$-${RANDOM}"
container_user="$(id -u):$(id -g)"
capture_pid=''
module_id=''

mkdir -p "$runtime_directory" "$home_directory" "$cache_directory"
chmod 700 "$runtime_directory"

pulse_server="${PULSE_SERVER:-}"
if [[ -z "$pulse_server" ]]; then
  pulse_server="$(pactl info | sed -n 's/^Server String: //p' | head -n 1)"
fi
if [[ "$pulse_server" != unix:* ]]; then
  echo 'The screen-reader test requires a local Unix PulseAudio socket so speech stays on the isolated virtual sink.' >&2
  exit 1
fi
pulse_socket_path="${pulse_server#unix:}"
pulse_socket_path="${pulse_socket_path%%,*}"
pulse_socket_path="${pulse_socket_path%%\?*}"
pulse_socket_directory="$(dirname "$pulse_socket_path")"
pulse_cookie="${PULSE_COOKIE:-${XDG_CONFIG_HOME:-$HOME/.config}/pulse/cookie}"

module_id="$(pactl load-module module-null-sink "sink_name=$sink_name" rate=48000 channels=2 sink_properties=device.description=PlayerScreenReaderAudit)"
if [[ ! "$module_id" =~ ^[0-9]+$ ]]; then
  echo 'Could not create the isolated screen-reader speech sink.' >&2
  exit 1
fi

cleanup() {
  docker stop --time 3 "$container_name" >/dev/null 2>&1 || true
  if [[ -n "$capture_pid" ]]; then
    kill -INT "$capture_pid" >/dev/null 2>&1 || true
    wait "$capture_pid" >/dev/null 2>&1 || true
  fi
  if [[ "$module_id" =~ ^[0-9]+$ ]]; then
    pactl unload-module "$module_id" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

parec --raw --format=s16le --rate=48000 --channels=2 --device="$sink_name.monitor" \
  >"$pcm_capture" &
capture_pid=$!
sleep 1
if ! kill -0 "$capture_pid" 2>/dev/null; then
  echo 'Could not record the isolated screen-reader speech sink.' >&2
  exit 1
fi

pnpm --filter @slskdn/web run build

export SLSKDN_PLAYER_A11Y_EVIDENCE_DIRECTORY="$evidence_directory"
export SLSKDN_PLAYER_A11Y_HOME_DIRECTORY="$home_directory"
export SLSKDN_PLAYER_A11Y_CACHE_DIRECTORY="$cache_directory"
export SLSKDN_PLAYER_A11Y_DEBUG_LOG="$debug_log"
export SLSKDN_PLAYER_A11Y_APPLICATIONS_LOG="$applications_log"
export SLSKDN_PLAYER_A11Y_CONTAINER="$container_name"
export SLSKDN_PLAYER_A11Y_CONTAINER_USER="$container_user"
export SLSKDN_PLAYER_A11Y_IMAGE="$image"
export SLSKDN_PLAYER_A11Y_PULSE_DIRECTORY="$pulse_socket_directory"
export SLSKDN_PLAYER_A11Y_PULSE_SERVER="$pulse_server"
export SLSKDN_PLAYER_A11Y_PULSE_COOKIE="$pulse_cookie"
export SLSKDN_PLAYER_A11Y_SINK="$sink_name"
export SLSKDN_PLAYER_A11Y_RUNTIME_DIRECTORY="$runtime_directory"
export SLSKDN_PLAYER_SCREEN_READER=1
export HEADLESS=false
export PULSE_SINK="$sink_name"
export XDG_CACHE_HOME="$cache_directory"
export XDG_RUNTIME_DIR="$runtime_directory"
export TMPDIR="$evidence_directory/tmp-$run_id"
mkdir -p "$TMPDIR"

# The session command must expand these variables after Xvfb and D-Bus are ready.
# shellcheck disable=SC2016
xvfb-run --auto-servernum --server-args='-screen 0 1440x1000x24 -ac' \
  dbus-run-session -- bash -c '
    set -euo pipefail
    dbus_socket_path="${DBUS_SESSION_BUS_ADDRESS#unix:path=}"
    dbus_socket_path="${dbus_socket_path%%,*}"
    if [[ "$dbus_socket_path" == "$DBUS_SESSION_BUS_ADDRESS" ]]; then
      echo "The screen-reader session bus must use a shareable Unix socket." >&2
      exit 1
    fi
    dbus_socket_directory="$(dirname "$dbus_socket_path")"
    container_mounts=(
      --mount "type=bind,src=$SLSKDN_PLAYER_A11Y_EVIDENCE_DIRECTORY,dst=$SLSKDN_PLAYER_A11Y_EVIDENCE_DIRECTORY"
      --mount "type=bind,src=$SLSKDN_PLAYER_A11Y_PULSE_DIRECTORY,dst=$SLSKDN_PLAYER_A11Y_PULSE_DIRECTORY"
      --mount "type=bind,src=$dbus_socket_directory,dst=$dbus_socket_directory"
      --mount type=bind,src=/tmp/.X11-unix,dst=/tmp/.X11-unix
    )
    export PULSE_COOKIE=''
    if [[ -f "$SLSKDN_PLAYER_A11Y_PULSE_COOKIE" ]]; then
      container_mounts+=(--mount "type=bind,src=$SLSKDN_PLAYER_A11Y_PULSE_COOKIE,dst=/tmp/player-a11y-pulse-cookie,readonly")
      export PULSE_COOKIE=/tmp/player-a11y-pulse-cookie
    fi

    docker run --detach --rm --name "$SLSKDN_PLAYER_A11Y_CONTAINER" \
      --user "$SLSKDN_PLAYER_A11Y_CONTAINER_USER" \
      "${container_mounts[@]}" \
      --env DISPLAY --env DBUS_SESSION_BUS_ADDRESS \
      --env XDG_CACHE_HOME --env XDG_RUNTIME_DIR \
      --env HOME="$SLSKDN_PLAYER_A11Y_HOME_DIRECTORY" \
      --env PULSE_SERVER="$SLSKDN_PLAYER_A11Y_PULSE_SERVER" \
      --env PULSE_SINK="$SLSKDN_PLAYER_A11Y_SINK" \
      --env PULSE_COOKIE \
      --entrypoint /bin/sleep "$SLSKDN_PLAYER_A11Y_IMAGE" infinity >/dev/null

    docker exec --detach --user "$SLSKDN_PLAYER_A11Y_CONTAINER_USER" \
      --env DISPLAY --env DBUS_SESSION_BUS_ADDRESS --env XDG_CACHE_HOME \
      --env XDG_RUNTIME_DIR --env HOME="$SLSKDN_PLAYER_A11Y_HOME_DIRECTORY" \
      --env PULSE_SERVER="$SLSKDN_PLAYER_A11Y_PULSE_SERVER" \
      --env PULSE_SINK="$SLSKDN_PLAYER_A11Y_SINK" --env PULSE_COOKIE \
      "$SLSKDN_PLAYER_A11Y_CONTAINER" /usr/libexec/at-spi-bus-launcher --launch-immediately --screen-reader=1
    docker exec --detach --user "$SLSKDN_PLAYER_A11Y_CONTAINER_USER" \
      --env DISPLAY --env DBUS_SESSION_BUS_ADDRESS --env XDG_CACHE_HOME \
      --env XDG_RUNTIME_DIR --env HOME="$SLSKDN_PLAYER_A11Y_HOME_DIRECTORY" \
      --env PULSE_SERVER="$SLSKDN_PLAYER_A11Y_PULSE_SERVER" \
      --env PULSE_SINK="$SLSKDN_PLAYER_A11Y_SINK" --env PULSE_COOKIE \
      "$SLSKDN_PLAYER_A11Y_CONTAINER" /usr/bin/orca --replace \
        --speech-system speechdispatcherfactory --debug-file="$SLSKDN_PLAYER_A11Y_DEBUG_LOG" --debug

    for attempt in $(seq 1 30); do
      if [[ -s "$SLSKDN_PLAYER_A11Y_DEBUG_LOG" ]]; then break; fi
      sleep 0.5
    done
    if [[ ! -s "$SLSKDN_PLAYER_A11Y_DEBUG_LOG" ]]; then
      echo "Orca did not start; inspect $SLSKDN_PLAYER_A11Y_DEBUG_LOG." >&2
      exit 1
    fi

    pnpm --filter @slskdn/web exec playwright test e2e/player.spec.ts \
      --grep @player-screen-reader --workers=1 --retries=0 --trace=off --reporter=line

    sleep 2
    docker exec --user "$SLSKDN_PLAYER_A11Y_CONTAINER_USER" \
      --env DISPLAY --env DBUS_SESSION_BUS_ADDRESS --env XDG_CACHE_HOME \
      --env XDG_RUNTIME_DIR --env HOME="$SLSKDN_PLAYER_A11Y_HOME_DIRECTORY" \
      --env PULSE_SERVER="$SLSKDN_PLAYER_A11Y_PULSE_SERVER" \
      --env PULSE_SINK="$SLSKDN_PLAYER_A11Y_SINK" --env PULSE_COOKIE \
      "$SLSKDN_PLAYER_A11Y_CONTAINER" /usr/bin/orca --list-apps \
      >"$SLSKDN_PLAYER_A11Y_APPLICATIONS_LOG"
  '

kill -INT "$capture_pid" >/dev/null 2>&1 || true
capture_status=0
wait "$capture_pid" || capture_status=$?
if [[ "$capture_status" -ne 0 && "$capture_status" -ne 130 ]]; then
  echo 'PulseAudio could not complete the screen-reader speech capture.' >&2
  exit 1
fi
capture_pid=''

if ! grep -Eiq 'Chromium|Chrome' "$applications_log"; then
  echo 'Orca did not register the Playwright Chromium browser as an accessible application.' >&2
  cat "$applications_log" >&2
  exit 1
fi
for spoken_text in "Now playing:" "Paused:" "Playback stopped."; do
  if ! grep -Fq "SPEECH OUTPUT: '$spoken_text" "$debug_log"; then
    echo "Orca did not send the expected player status to speech: $spoken_text" >&2
    exit 1
  fi
done

python3 - "$pcm_capture" <<'PY'
from array import array
import math
import os
import sys

with open(sys.argv[1], "rb") as capture_file:
    samples = array("h")
    samples.frombytes(capture_file.read())
if sys.byteorder != "little":
    samples.byteswap()
if len(samples) % 2:
    raise SystemExit("Captured Orca speech has a partial stereo frame.")

sample_rate = 48_000
samples_per_second = sample_rate * 2
active_windows = 0
for offset in range(0, len(samples) - samples_per_second + 1, samples_per_second):
    window = samples[offset:offset + samples_per_second]
    rms = math.sqrt(sum(sample * sample for sample in window) / len(window))
    if rms >= 150:
        active_windows += 1

seconds = len(samples) / samples_per_second
peak = max((abs(sample) for sample in samples), default=0)
print(f"Orca spoke for {seconds:.1f} captured seconds; {active_windows} one-second windows contain speech; peak {peak}.")
if seconds < 2 or active_windows < 3 or peak < 300:
    raise SystemExit("The isolated screen-reader sink did not capture enough synthesized speech.")
PY

echo "Orca log: $debug_log"
echo "Orca application list: $applications_log"
echo "Isolated speech PCM: $pcm_capture"
