#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

image='slskdn-player-a11y:orca-clean'
screen_reader_browser="${SLSKDN_PLAYER_A11Y_BROWSER:-chromium}"
case "$screen_reader_browser" in
  chromium|firefox|webkit) ;;
  *)
    echo "Unsupported screen-reader browser: $screen_reader_browser" >&2
    exit 1
    ;;
esac

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
runtime_directory=''
home_directory="$evidence_directory/home-$run_id"
debug_log="$evidence_directory/orca-$run_id.log"
startup_error_log="$evidence_directory/orca-startup-$run_id.log"
pcm_capture="$evidence_directory/orca-speech-$run_id.s16le"
pcm_offset_file="$evidence_directory/orca-speech-offset-$run_id.txt"
sink_name="slskdn_player_a11y_$$"
container_name="slskdn-player-a11y-$$-${RANDOM}"
container_uid="$(id -u)"
container_gid="$(id -g)"
container_user="$container_uid:$container_gid"
capture_pid=''
playwright_tmp_directory=''
module_id=''

container_cache_directory='/tmp/slskdn-player-a11y-cache'
mkdir -p "$home_directory"

pulse_server="${PULSE_SERVER:-}"
if [[ -z "$pulse_server" ]]; then
  pulse_server="$(pactl info | sed -n 's/^Server String: //p' | head -n 1)"
fi
if [[ "$pulse_server" == /* ]]; then
  pulse_server="unix:$pulse_server"
fi
if [[ "$pulse_server" != unix:* ]]; then
  echo 'The screen-reader test requires a local Unix PulseAudio socket so speech stays on the isolated virtual sink.' >&2
  exit 1
fi
pulse_socket_path="${pulse_server#unix:}"
pulse_socket_path="${pulse_socket_path%%,*}"
pulse_socket_path="${pulse_socket_path%%\?*}"
pulse_socket_directory="$(dirname "$pulse_socket_path")"

module_id="$(pactl load-module module-null-sink "sink_name=$sink_name" rate=48000 channels=2 sink_properties=device.description=PlayerScreenReaderAudit)"
if [[ ! "$module_id" =~ ^[0-9]+$ ]]; then
  echo 'Could not create the isolated screen-reader speech sink.' >&2
  exit 1
fi
browser_sink_name="slskdn_player_audio_$$"
browser_module_id="$(pactl load-module module-null-sink "sink_name=$browser_sink_name" rate=48000 channels=2 sink_properties=device.description=PlayerBrowserAudit)"
if [[ ! "$browser_module_id" =~ ^[0-9]+$ ]]; then
  pactl unload-module "$module_id" >/dev/null 2>&1 || true
  echo 'Could not create the isolated browser playback sink.' >&2
  exit 1
fi
pulse_cookie="${PULSE_COOKIE:-${XDG_CONFIG_HOME:-$HOME/.config}/pulse/cookie}"

cleanup() {
  docker stop --time 3 "$container_name" >/dev/null 2>&1 || true
  if [[ -n "$capture_pid" ]]; then
    kill -INT "$capture_pid" >/dev/null 2>&1 || true
    wait "$capture_pid" >/dev/null 2>&1 || true
  fi
  if [[ "$module_id" =~ ^[0-9]+$ ]]; then
    pactl unload-module "$module_id" >/dev/null 2>&1 || true
  fi
  if [[ "$browser_module_id" =~ ^[0-9]+$ ]]; then
    pactl unload-module "$browser_module_id" >/dev/null 2>&1 || true
  fi
  if [[ -n "$playwright_tmp_directory" ]]; then
    rm -rf -- "$playwright_tmp_directory"
  fi
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
playwright_tmp_directory="$(mktemp -d /tmp/slskdn-player-a11y.XXXXXX)"
runtime_directory="$playwright_tmp_directory"
{
  printf '%s\n' 'root:x:0:0:root:/root:/bin/sh'
  if [[ "$container_uid" != 0 ]]; then
    printf 'player-a11y:x:%s:%s:Player Screen Reader:%s:/bin/sh\n' \
      "$container_uid" "$container_gid" "$home_directory"
  fi
} >"$runtime_directory/passwd"
{
  printf '%s\n' 'root:x:0:'
  if [[ "$container_gid" != 0 ]]; then
    printf 'player-a11y:x:%s:\n' "$container_gid"
  fi
} >"$runtime_directory/group"

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
export SLSKDN_PLAYER_A11Y_CACHE_DIRECTORY="$container_cache_directory"
export SLSKDN_PLAYER_A11Y_DEBUG_LOG="$debug_log"
export SLSKDN_PLAYER_A11Y_STARTUP_ERROR_LOG="$startup_error_log"
export SLSKDN_PLAYER_A11Y_PCM_CAPTURE="$pcm_capture"
export SLSKDN_PLAYER_A11Y_PCM_OFFSET_FILE="$pcm_offset_file"
export SLSKDN_PLAYER_A11Y_CONTAINER="$container_name"
export SLSKDN_PLAYER_A11Y_CONTAINER_USER="$container_user"
export SLSKDN_PLAYER_A11Y_BROWSER="$screen_reader_browser"
export SLSKDN_PLAYER_A11Y_IMAGE="$image"
export SLSKDN_PLAYER_A11Y_PULSE_DIRECTORY="$pulse_socket_directory"
export SLSKDN_PLAYER_A11Y_PULSE_SERVER="$pulse_server"
export SLSKDN_PLAYER_A11Y_PULSE_COOKIE="$pulse_cookie"
export SLSKDN_PLAYER_A11Y_SINK="$sink_name"
export SLSKDN_PLAYER_A11Y_RUNTIME_DIRECTORY="$runtime_directory"
export SLSKDN_PLAYER_SCREEN_READER=1
export HEADLESS=false
export PULSE_SINK="$browser_sink_name"
export XDG_RUNTIME_DIR="$runtime_directory"
export TMPDIR="$playwright_tmp_directory"

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
      --mount "type=bind,src=$SLSKDN_PLAYER_A11Y_RUNTIME_DIRECTORY,dst=$SLSKDN_PLAYER_A11Y_RUNTIME_DIRECTORY"
      --mount "type=bind,src=$SLSKDN_PLAYER_A11Y_RUNTIME_DIRECTORY/passwd,dst=/etc/passwd,readonly"
      --mount "type=bind,src=$SLSKDN_PLAYER_A11Y_RUNTIME_DIRECTORY/group,dst=/etc/group,readonly"
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
      --env XDG_CACHE_HOME="$SLSKDN_PLAYER_A11Y_CACHE_DIRECTORY" --env XDG_RUNTIME_DIR \
      --env HOME="$SLSKDN_PLAYER_A11Y_HOME_DIRECTORY" \
      --env PULSE_SERVER="$SLSKDN_PLAYER_A11Y_PULSE_SERVER" \
      --env PULSE_SINK="$SLSKDN_PLAYER_A11Y_SINK" \
      --env PULSE_COOKIE \
      --entrypoint /bin/sleep "$SLSKDN_PLAYER_A11Y_IMAGE" infinity >/dev/null

    container_exec_options=(
      --user "$SLSKDN_PLAYER_A11Y_CONTAINER_USER"
      --env DISPLAY
      --env DBUS_SESSION_BUS_ADDRESS
      --env XDG_CACHE_HOME="$SLSKDN_PLAYER_A11Y_CACHE_DIRECTORY"
      --env XDG_RUNTIME_DIR
      --env HOME="$SLSKDN_PLAYER_A11Y_HOME_DIRECTORY"
      --env PULSE_SERVER="$SLSKDN_PLAYER_A11Y_PULSE_SERVER"
      --env PULSE_SINK="$SLSKDN_PLAYER_A11Y_SINK"
      --env PULSE_COOKIE
    )

    docker exec --detach "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
      /usr/bin/speech-dispatcher --run-daemon --timeout 0
    speech_dispatcher_ready=false
    for attempt in $(seq 1 30); do
      if docker exec "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
        /usr/bin/spd-say --wait --application-name=PlayerScreenReaderAudit \
        "Speech engine audio check" >/dev/null 2>&1; then
        speech_dispatcher_ready=true
        break
      fi
      sleep 0.5
    done
    if [[ "$speech_dispatcher_ready" != true ]]; then
      echo "Speech Dispatcher did not produce its isolated audio check." >&2
      exit 1
    fi
    sleep 0.5
    capture_offset="$(wc -c < "$SLSKDN_PLAYER_A11Y_PCM_CAPTURE")"
    printf "%s\n" "$capture_offset" >"$SLSKDN_PLAYER_A11Y_PCM_OFFSET_FILE"

    docker exec --detach "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
      /usr/libexec/at-spi-bus-launcher --launch-immediately --screen-reader=1

    a11y_bus_address=''
    a11y_bus_ready=false
    for attempt in $(seq 1 30); do
      a11y_bus_reply="$(docker exec "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
        /usr/bin/dbus-send --session --print-reply --dest=org.a11y.Bus \
        /org/a11y/bus org.a11y.Bus.GetAddress 2>/dev/null || true)"
      a11y_bus_address="${a11y_bus_reply#*string \"}"
      a11y_bus_address="${a11y_bus_address%%\"*}"
      if [[ -n "$a11y_bus_address" ]] && \
        docker exec "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
          /usr/bin/dbus-send --bus="$a11y_bus_address" --print-reply \
          --dest=org.freedesktop.DBus /org/freedesktop/DBus \
          org.freedesktop.DBus.ListNames >/dev/null 2>&1; then
        a11y_bus_ready=true
        break
      fi
      sleep 0.5
    done
    if [[ "$a11y_bus_ready" != true ]]; then
      echo "The AT-SPI accessibility bus did not become ready." >&2
      exit 1
    fi

    docker exec --detach "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
      /bin/sh -c \
      "exec /usr/bin/orca --replace --speech-system speechdispatcherfactory --debug-file=\"\$1\" --debug >\"\$2\" 2>&1" \
      player-orca "$SLSKDN_PLAYER_A11Y_DEBUG_LOG" "$SLSKDN_PLAYER_A11Y_STARTUP_ERROR_LOG"

    for attempt in $(seq 1 30); do
      if [[ -s "$SLSKDN_PLAYER_A11Y_DEBUG_LOG" ]]; then break; fi
      sleep 0.5
    done
    if [[ ! -s "$SLSKDN_PLAYER_A11Y_DEBUG_LOG" ]]; then
      echo "Orca did not start; inspect $SLSKDN_PLAYER_A11Y_DEBUG_LOG." >&2
      if [[ -s "$SLSKDN_PLAYER_A11Y_STARTUP_ERROR_LOG" ]]; then
        cat "$SLSKDN_PLAYER_A11Y_STARTUP_ERROR_LOG" >&2
      fi
      docker exec "${container_exec_options[@]}" "$SLSKDN_PLAYER_A11Y_CONTAINER" \
        /usr/bin/pgrep --full /usr/bin/orca >&2 || true
      exit 1
    fi

    pnpm --filter @slskdn/web exec playwright test e2e/player.spec.ts \
      --grep @player-screen-reader --browser="$SLSKDN_PLAYER_A11Y_BROWSER" \
      --workers=1 --retries=0 --trace=off --reporter=line

    sleep 2
  '

kill -INT "$capture_pid" >/dev/null 2>&1 || true
capture_status=0
wait "$capture_pid" || capture_status=$?
if [[ "$capture_status" -ne 0 && "$capture_status" -ne 130 ]]; then
  echo 'PulseAudio could not complete the screen-reader speech capture.' >&2
  exit 1
fi
capture_pid=''

if ! grep -Fq "SPEECH OUTPUT: 'Now playing: Player runtime first.'" "$debug_log"; then
  echo 'Orca did not process Player content in the selected browser while the page was open.' >&2
  exit 1
fi
if grep -Eiq 'Speech Dispatcher service failed to connect|No speech server for factory' "$debug_log"; then
  echo 'Orca logged a Speech Dispatcher connection failure.' >&2
  exit 1
fi
for spoken_text in "Now playing: Player runtime first." "Paused: Player runtime first." "Playback stopped."; do
  if ! grep -Fq "SPEECH OUTPUT: '$spoken_text'" "$debug_log"; then
    echo "Orca did not send the expected player status to speech: $spoken_text" >&2
    exit 1
  fi
done

python3 - "$pcm_capture" "$pcm_offset_file" <<'PY'
from array import array
import math
import os
import sys

with open(sys.argv[1], "rb") as capture_file:
    with open(sys.argv[2], encoding="utf8") as offset_file:
        capture_file.seek(int(offset_file.read().strip()))
    samples = array("h")
    samples.frombytes(capture_file.read())
if sys.byteorder != "little":
    samples.byteswap()
if len(samples) % 2:
    raise SystemExit("Captured Orca speech has a partial stereo frame.")

samples_per_tenth = 48_000 * 2 // 10
active_windows = 0
for offset in range(0, len(samples) - samples_per_tenth + 1, samples_per_tenth):
    window = samples[offset:offset + samples_per_tenth]
    rms = math.sqrt(sum(sample * sample for sample in window) / len(window))
    if rms >= 150:
        active_windows += 1

seconds = len(samples) / (48_000 * 2)
peak = max((abs(sample) for sample in samples), default=0)
print(f"Orca speech capture contains {active_windows} active 100 ms windows over {seconds:.1f} seconds; peak {peak}.")
if seconds < 2 or active_windows < 15 or peak < 300:
    raise SystemExit("The isolated screen-reader sink did not capture enough synthesized speech.")
PY

echo "Orca log: $debug_log"
echo "Isolated speech PCM: $pcm_capture"
