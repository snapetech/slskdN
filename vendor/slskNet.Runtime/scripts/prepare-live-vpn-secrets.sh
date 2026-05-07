#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
secret_dir="${SLSKNET_RUNTIME_VPN_SECRET_DIR:-$repo_root/.secrets/vpn}"
manifest="$secret_dir/live-vpn.env"

mkdir -p "$secret_dir"
chmod 700 "$repo_root/.secrets" "$secret_dir"

write_config_from_env() {
    local index="$1"
    local var_name="SLSKNET_RUNTIME_VPN_CONFIG_$index"
    local output="$secret_dir/slsknet-$index.conf"
    local value="${!var_name:-}"

    if [[ -n "$value" ]]; then
        umask 077
        printf '%s\n' "$value" >"$output"
        chmod 600 "$output"
    fi
}

write_config_from_env 1
write_config_from_env 2
write_config_from_env 3

if [[ -n "${SLSKNET_RUNTIME_VPN_MANIFEST:-}" ]]; then
    umask 077
    printf '%s\n' "$SLSKNET_RUNTIME_VPN_MANIFEST" >"$manifest"
    chmod 600 "$manifest"
elif [[ ! -f "$manifest" ]]; then
    umask 077
    cat >"$manifest" <<'EOF'
SLSKNET_RUNTIME_VPN_CONFIG_1=.secrets/vpn/slsknet-1.conf
SLSKNET_RUNTIME_VPN_CONFIG_2=.secrets/vpn/slsknet-2.conf
SLSKNET_RUNTIME_VPN_CONFIG_3=.secrets/vpn/slsknet-3.conf
SLSKNET_RUNTIME_VPN_NATPMP_CONFIGS="1 3 5 6"
SLSKNET_RUNTIME_VPN_EGRESS_ONLY_CONFIGS="2"
SLSKNET_RUNTIME_VPN_NATPMP_GATEWAY=10.2.0.1
EOF
    chmod 600 "$manifest"
fi

missing=0
for index in 1 2 3; do
    path="$secret_dir/slsknet-$index.conf"
    if [[ ! -f "$path" ]]; then
        printf 'Missing live VPN config %s at %s\n' "$index" "$path" >&2
        missing=1
    fi
done

if [[ "$missing" == "1" ]]; then
    exit 1
fi

printf 'Live VPN secrets ready: %s\n' "$secret_dir"
