#!/usr/bin/env bash
set -euo pipefail

if (( $# < 1 )); then
    echo "usage: $0 <command> [args...]" >&2
    exit 2
fi

if [[ "${SLSKDN_FULL_INSTANCE_VPN_CLAIM_PORT_FORWARDING:-0}" == "1" ]]; then
    port="${SLSKDN_VPN_TEST_FORWARD_PORT:-}"
    private_port="$port"
    gateway="${SLSKDN_VPN_TEST_FORWARD_GATEWAY:-10.2.0.1}"
    lifetime="${SLSKDN_VPN_TEST_FORWARD_LIFETIME:-60}"
    attempts="${SLSKDN_VPN_TEST_FORWARD_ATTEMPTS:-20}"
    renew_seconds="${SLSKDN_VPN_TEST_FORWARD_RENEW_SECONDS:-45}"
    config_path=""

    if [[ -z "$port" ]]; then
        echo "SLSKDN_VPN_TEST_FORWARD_PORT is required when VPN port forwarding is enabled" >&2
        exit 2
    fi

    if ! command -v natpmpc >/dev/null 2>&1; then
        echo "natpmpc is required for VPN port-forward claiming" >&2
        exit 2
    fi

    for (( i = 1; i < $#; i++ )); do
        if [[ "${!i}" == "--config" ]]; then
            next=$((i + 1))
            config_path="${!next:-}"
            break
        fi
    done

    for (( attempt = 1; attempt <= attempts; attempt++ )); do
        output="$(timeout 8 natpmpc -g "$gateway" -a 0 "$port" tcp "$lifetime" 2>&1 || true)"
        printf '%s\n' "$output" >&2

        mapped="$(sed -nE 's/.*Mapped public port ([0-9]+).*/\1/p' <<<"$output" | tail -1)"
        public_ip="$(sed -nE 's/.*Public IP address[[:space:]]*:[[:space:]]*([^[:space:]]+).*/\1/p' <<<"$output" | tail -1)"
        if [[ -n "$mapped" ]]; then
            port="$mapped"
            if [[ -n "$config_path" && -f "$config_path" ]]; then
                sed -i -E "s/^([[:space:]]*listen_port:[[:space:]]*)[0-9]+[[:space:]]*$/\\1$port/" "$config_path"
            fi

            if [[ "$private_port" != "$port" ]]; then
                iptables_command=(iptables)
                if (( EUID != 0 )) && command -v sudo >/dev/null 2>&1; then
                    iptables_command=(sudo iptables)
                fi
                "${iptables_command[@]}" -t nat -A PREROUTING -p tcp --dport "$private_port" -j REDIRECT --to-ports "$port"
                "${iptables_command[@]}" -t nat -A OUTPUT -p tcp -d 127.0.0.1 --dport "$private_port" -j REDIRECT --to-ports "$port" || true
            fi

            if [[ -n "${SLSKDN_VPN_TEST_FORWARD_STATE_FILE:-}" ]]; then
                {
                    printf 'local_port=%s\n' "$private_port"
                    printf 'target_port=%s\n' "$port"
                    printf 'proto=tcp\n'
                    printf 'public_port=%s\n' "$mapped"
                    printf 'public_ip=%s\n' "$public_ip"
                    printf 'namespace=%s\n' "${SLSKDN_VPN_TEST_FORWARD_NAMESPACE:-}"
                } >"$SLSKDN_VPN_TEST_FORWARD_STATE_FILE"
            fi
            (
                while true; do
                    natpmpc -g "$gateway" -a "$port" "$private_port" tcp "$lifetime" >/dev/null 2>&1 || true
                    sleep "$renew_seconds"
                done
            ) &
            break
        fi

        if (( attempt == attempts )); then
            echo "VPN provider did not grant a TCP mapping for private port $port" >&2
            exit 76
        fi

        sleep 1
    done
fi

exec "$@"
