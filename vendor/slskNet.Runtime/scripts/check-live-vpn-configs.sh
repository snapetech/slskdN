#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
"$repo_root/scripts/prepare-live-vpn-secrets.sh" >/dev/null

manifest="${SLSKNET_RUNTIME_VPN_SECRET_DIR:-$repo_root/.secrets/vpn}/live-vpn.env"
set -a
# shellcheck disable=SC1090
source "$manifest"
set +a

extract_value() {
    local key="$1"
    local file="$2"
    awk -v key="$key" '
        $0 ~ "^[[:space:]]*" key "[[:space:]]*=" {
            value=$0
            sub("^[[:space:]]*" key "[[:space:]]*=[[:space:]]*", "", value)
            sub(/[[:space:]]*$/, "", value)
            print value
            exit
        }
    ' "$file"
}

resolve_path() {
    local path="$1"
    if [[ "$path" == /* ]]; then
        printf '%s' "$path"
    else
        printf '%s/%s' "$repo_root" "$path"
    fi
}

cleanup_iface() {
    local iface="$1"
    sudo ip route del 10.2.0.1/32 dev "$iface" 2>/dev/null || true
    sudo ip link del "$iface" 2>/dev/null || true
    rm -f "/tmp/${iface}.key"
}

check_handshake() {
    local index="$1"
    local mode="$2"
    local var_name="SLSKNET_RUNTIME_VPN_CONFIG_$index"
    local config="${!var_name:-}"
    local iface="srtvpn$index"
    local key_file="/tmp/${iface}.key"

    if [[ -z "$config" ]]; then
        printf 'VPN config %s is not declared in %s\n' "$index" "$manifest" >&2
        return 1
    fi

    config="$(resolve_path "$config")"
    if [[ ! -f "$config" ]]; then
        printf 'VPN config %s is missing at %s\n' "$index" "$config" >&2
        return 1
    fi

    cleanup_iface "$iface"
    trap 'cleanup_iface "$iface"' RETURN

    extract_value PrivateKey "$config" | tr -d '\r\n' >"$key_file"
    chmod 600 "$key_file"

    local address
    local public_key
    local endpoint
    address="$(extract_value Address "$config" | cut -d, -f1 | xargs)"
    public_key="$(extract_value PublicKey "$config" | tr -d '\r\n')"
    endpoint="$(extract_value Endpoint "$config" | tr -d '\r\n')"

    sudo ip link add "$iface" type wireguard
    sudo ip addr add "$address" dev "$iface"
    sudo wg set "$iface" private-key "$key_file" peer "$public_key" endpoint "$endpoint" allowed-ips 0.0.0.0/0 persistent-keepalive 1
    sudo ip link set mtu 1420 up dev "$iface"
    sudo ip route replace 10.2.0.1/32 dev "$iface"

    sudo timeout 3 ping -c1 -W1 -I "$iface" 10.2.0.1 >/dev/null 2>&1 || true
    sleep "${SLSKNET_RUNTIME_VPN_HANDSHAKE_WAIT_SECONDS:-3}"

    if ! sudo wg show "$iface" latest-handshakes | awk '$2 != 0 { found=1 } END { exit found ? 0 : 1 }'; then
        printf 'FAIL vpn-config-%s %s endpoint=%s handshake=none\n' "$index" "$mode" "$endpoint" >&2
        return 1
    fi

    printf 'PASS vpn-config-%s %s endpoint=%s handshake=ok\n' "$index" "$mode" "$endpoint"
}

status=0
for index in ${SLSKNET_RUNTIME_VPN_NATPMP_CONFIGS:-}; do
    check_handshake "$index" natpmp-capable || status=1
done

for index in ${SLSKNET_RUNTIME_VPN_EGRESS_ONLY_CONFIGS:-}; do
    check_handshake "$index" egress-only || status=1
done

exit "$status"
