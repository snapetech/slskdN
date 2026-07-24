#!/usr/bin/env bash

set -euo pipefail

package_version="${1:?Usage: $0 PACKAGE_VERSION UBUNTU_SERIES}"
ubuntu_series="${2:?Usage: $0 PACKAGE_VERSION UBUNTU_SERIES}"

archive_owner="${LAUNCHPAD_ARCHIVE_OWNER:-keefshape}"
archive_name="${LAUNCHPAD_ARCHIVE_NAME:-slskdn}"
source_name="${LAUNCHPAD_SOURCE_NAME:-slskdn}"
api_root="${LAUNCHPAD_API_ROOT:-https://api.launchpad.net/1.0}"
timeout_seconds="${LAUNCHPAD_PUBLICATION_TIMEOUT_SECONDS:-5400}"
poll_seconds="${LAUNCHPAD_PUBLICATION_POLL_SECONDS:-15}"
archive_api="${api_root}/~${archive_owner}/+archive/ubuntu/${archive_name}"
series_suffix="/ubuntu/${ubuntu_series}"
deadline=$((SECONDS + timeout_seconds))
last_status=""

require_command() {
    command -v "$1" >/dev/null 2>&1 || {
        echo "Required command is unavailable: $1" >&2
        exit 1
    }
}

query() {
    curl \
        --retry 5 \
        --retry-all-errors \
        --retry-delay 2 \
        --connect-timeout 30 \
        --max-time 120 \
        -fsSG "$@"
}

report_status() {
    if [[ "$1" != "$last_status" ]]; then
        echo "$1"
        last_status="$1"
    fi
}

fail_if_terminal_source_state() {
    case "$1" in
        Superseded|Deleted|Obsolete)
            echo "Launchpad source ${package_version} entered terminal state '$1' before its binary was published." >&2
            exit 1
            ;;
    esac
}

fail_if_terminal_build_state() {
    case "$1" in
        "Failed to build"|"Failed to upload"|"Chroot problem"|"Build for superseded Source"|"Cancelled build")
            echo "Launchpad build for ${package_version} entered terminal state '$1'." >&2
            exit 1
            ;;
    esac
}

require_command curl
require_command jq

echo "Waiting for slskdn ${package_version} to publish for Ubuntu ${ubuntu_series}."

while (( SECONDS < deadline )); do
    if ! source_json="$(query "$archive_api" \
        --data-urlencode 'ws.op=getPublishedSources' \
        --data-urlencode "source_name=${source_name}" \
        --data-urlencode "version=${package_version}" \
        --data-urlencode 'exact_match=true')"; then
        report_status "Launchpad source query failed after retries; polling again."
        sleep "$poll_seconds"
        continue
    fi

    source_entry="$(jq -c \
        --arg version "$package_version" \
        --arg series_suffix "$series_suffix" \
        '.entries[] | select(.source_package_version == $version and (.distro_series_link | endswith($series_suffix)))' \
        <<<"$source_json" | head -1)"

    if [[ -z "$source_entry" ]]; then
        report_status "Launchpad has not accepted the exact source version yet."
        sleep "$poll_seconds"
        continue
    fi

    source_status="$(jq -r '.status' <<<"$source_entry")"
    source_link="$(jq -r '.self_link' <<<"$source_entry")"
    fail_if_terminal_source_state "$source_status"

    if ! builds_json="$(query "$source_link" --data-urlencode 'ws.op=getBuilds')"; then
        report_status "Source is ${source_status}; its build query failed after retries."
        sleep "$poll_seconds"
        continue
    fi

    mapfile -t build_states < <(jq -r \
        --arg series_suffix "$series_suffix" \
        '.entries[] | select(.distro_series_link | endswith($series_suffix)) | .buildstate' \
        <<<"$builds_json")

    if (( ${#build_states[@]} == 0 )); then
        report_status "Source is ${source_status}; Launchpad has not scheduled its binary build yet."
        sleep "$poll_seconds"
        continue
    fi

    all_builds_succeeded=true
    for build_state in "${build_states[@]}"; do
        fail_if_terminal_build_state "$build_state"
        if [[ "$build_state" != "Successfully built" ]]; then
            all_builds_succeeded=false
        fi
    done

    build_summary="$(printf '%s\n' "${build_states[@]}" | sort -u | paste -sd ', ' -)"
    if [[ "$all_builds_succeeded" != true ]]; then
        report_status "Source is ${source_status}; build state: ${build_summary}."
        sleep "$poll_seconds"
        continue
    fi

    if ! binaries_json="$(query "$archive_api" \
        --data-urlencode 'ws.op=getPublishedBinaries' \
        --data-urlencode "binary_name=${source_name}" \
        --data-urlencode "version=${package_version}" \
        --data-urlencode 'exact_match=true')"; then
        report_status "Build succeeded; the binary publication query failed after retries."
        sleep "$poll_seconds"
        continue
    fi

    published_count="$(jq \
        --arg version "$package_version" \
        --arg series_suffix "$series_suffix" \
        '[.entries[] | select(.binary_package_version == $version and .status == "Published" and (.distro_arch_series_link | contains($series_suffix)))] | length' \
        <<<"$binaries_json")"

    if (( published_count > 0 )); then
        echo "Launchpad published slskdn ${package_version} for Ubuntu ${ubuntu_series}."
        exit 0
    fi

    report_status "Build succeeded; waiting for the exact binary publication."
    sleep "$poll_seconds"
done

echo "Timed out after ${timeout_seconds}s waiting for slskdn ${package_version} to publish for Ubuntu ${ubuntu_series}." >&2
exit 1
