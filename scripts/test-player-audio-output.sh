#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

for executable in pactl parec pnpm python3; do
  if ! command -v "$executable" >/dev/null 2>&1; then
    echo "Required executable is unavailable: $executable" >&2
    exit 1
  fi
done

default_sink_name="slskdn_player_audit_default_$$"
route_sink_name="slskdn_player_audit_route_$$"
temporary_directory="$(mktemp -d "${TMPDIR:-/tmp}/slskdn-player-audio.XXXXXX")"
capture_path="$temporary_directory/output.s16le"
capture_error_path="$temporary_directory/parec.stderr"
alsa_config_path="$temporary_directory/asound.conf"
module_id=''
route_module_id=''
capture_pid=''

cleanup() {
  if [[ -n "$capture_pid" ]]; then
    kill -INT "$capture_pid" >/dev/null 2>&1 || true
    wait "$capture_pid" >/dev/null 2>&1 || true
  fi
  if [[ "$route_module_id" =~ ^[0-9]+$ ]]; then
    pactl unload-module "$route_module_id" >/dev/null 2>&1 || true
  fi
  if [[ "$module_id" =~ ^[0-9]+$ ]]; then
    pactl unload-module "$module_id" >/dev/null 2>&1 || true
  fi
  rm -rf "$temporary_directory"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

module_id="$(pactl load-module module-null-sink "sink_name=$default_sink_name" rate=48000 channels=2 sink_properties=device.description=PlayerAuditDefault)"
if [[ ! "$module_id" =~ ^[0-9]+$ ]]; then
  echo 'Could not create the isolated PulseAudio null sink.' >&2
  exit 1
fi
route_module_id="$(pactl load-module module-null-sink "sink_name=$route_sink_name" rate=48000 channels=2 sink_properties=device.description=PlayerAuditRoute)"
if [[ ! "$route_module_id" =~ ^[0-9]+$ ]]; then
  echo 'Could not create the selected-output PulseAudio null sink.' >&2
  exit 1
fi
cat >"$alsa_config_path" <<'EOF'
pcm.!default {
  type null
}
EOF

pnpm --filter @slskdn/web run build
parec --raw --format=s16le --rate=48000 --channels=2 --device="$route_sink_name.monitor" \
  >"$capture_path" 2>"$capture_error_path" &
capture_pid=$!
sleep 1
if ! kill -0 "$capture_pid" 2>/dev/null; then
  cat "$capture_error_path" >&2
  exit 1
fi

SLSKDN_PLAYER_AUDIO_OUTPUT=1 \
SLSKDN_PLAYER_AUDIO_SINK_LABEL=PlayerAuditRoute \
PULSE_SINK="$default_sink_name" ALSA_CONFIG_PATH="$alsa_config_path" pnpm --filter @slskdn/web exec playwright test \
  e2e/player-audio-output.spec.ts --workers=1 --retries=0 --trace=off --reporter=line

kill -INT "$capture_pid" >/dev/null 2>&1 || true
capture_status=0
wait "$capture_pid" || capture_status=$?
if [[ "$capture_status" -ne 0 && "$capture_status" -ne 130 ]]; then
  cat "$capture_error_path" >&2
  exit 1
fi
capture_pid=''

python3 - "$capture_path" <<'PY'
from array import array
import math
import os
import sys

capture_path = sys.argv[1]
with open(capture_path, "rb") as capture_file:
    pcm = array("h")
    pcm.frombytes(capture_file.read())
if sys.byteorder != "little":
    pcm.byteswap()
if len(pcm) % 2:
    raise SystemExit("Captured PulseAudio data has a partial stereo frame.")

frames_per_second = 48_000
samples_per_window = frames_per_second * 2
window_rms = []
for offset in range(0, len(pcm) - samples_per_window + 1, samples_per_window):
    samples = pcm[offset:offset + samples_per_window]
    window_rms.append(math.sqrt(sum(sample * sample for sample in samples) / len(samples)))

active_windows = sum(rms >= 150 for rms in window_rms)
peak = max((abs(sample) for sample in pcm), default=0)
seconds = len(pcm) / (frames_per_second * 2)
print(f"Captured {seconds:.1f} seconds; {active_windows} one-second windows contain PCM; peak {peak}.")
if seconds < 7 or active_windows < 4 or peak < 300:
    raise SystemExit("The isolated output capture did not contain enough generated audio.")
PY
