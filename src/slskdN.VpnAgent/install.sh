#!/usr/bin/env bash
set -euo pipefail

ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
MODE=${1:-install}
INSTALL_DIR=/usr/local/lib/slskdN-vpn-agent
AGENT_LINK=/usr/local/bin/slskdN-vpn-agent
ENV_FILE=/etc/default/slskdN-vpn-agent

usage() {
  cat <<'USAGE'
Usage: sudo ./install.sh [install|relay|verify|--help]

install  Install the Linux VPN agent and enable its systemd services.
relay    Install the self-hosted relay companion service.
verify   Check the currently installed VPN agent setup.

The release ZIP contains a ready-to-run agent, so it does not need the .NET SDK.
From a source checkout, install mode builds the self-contained agent with dotnet.
USAGE
}

if [[ "$MODE" == "--help" || "$MODE" == "help" || "$MODE" == "-h" ]]; then
  usage
  exit 0
fi

require_root() {
  if [[ ${EUID} -ne 0 ]]; then
    echo "Run as root: sudo $0" >&2
    exit 1
  fi
}

require_command() {
  command -v "$1" >/dev/null || {
    echo "Missing required command: $1" >&2
    exit 2
  }
}

install_file() {
  local mode=$1 src=$2 dst=$3
  install -D -m "$mode" "$src" "$dst"
}

install_agent_binary() {
  install -d -m 0755 "$INSTALL_DIR"
  if [[ -x "$ROOT/slskdN-vpn-agent" ]]; then
    install -m 0755 "$ROOT/slskdN-vpn-agent" "$INSTALL_DIR/slskdN-vpn-agent"
  else
    require_command dotnet
    [[ -f "$ROOT/slskdN-vpn-agent.csproj" ]] || {
      echo "No packaged agent binary or source project found in $ROOT" >&2
      exit 2
    }
    dotnet publish "$ROOT/slskdN-vpn-agent.csproj" \
      -c Release \
      -r linux-x64 \
      --self-contained true \
      -p:PublishSingleFile=true \
      -o "$INSTALL_DIR" >/dev/null
  fi

  ln -sfn "$INSTALL_DIR/slskdN-vpn-agent" "$AGENT_LINK"
}

resolve_app_service() {
  if [[ -n "${SLSKDN_SERVICE_NAME:-}" ]]; then
    printf '%s\n' "$SLSKDN_SERVICE_NAME"
  elif systemctl cat slskd.service >/dev/null 2>&1; then
    printf 'slskd\n'
  elif systemctl cat slskdN.service >/dev/null 2>&1; then
    printf 'slskdN\n'
  else
    printf 'slskdN\n'
  fi
}

resolve_app_user() {
  local app_service=$1 configured_user
  if [[ -n "${SLSKDN_SERVICE_USER:-}" ]]; then
    printf '%s\n' "$SLSKDN_SERVICE_USER"
    return
  fi

  if systemctl cat "${app_service}.service" >/dev/null 2>&1; then
    configured_user=$(systemctl show --property=User --value "${app_service}.service" 2>/dev/null || true)
    printf '%s\n' "${configured_user:-root}"
    return
  fi

  if getent passwd slskd >/dev/null; then
    printf 'slskd\n'
  else
    printf 'slskdN\n'
  fi
}

safe_env_value() {
  local name=$1 value=$2
  if [[ ! "$value" =~ ^[A-Za-z0-9_./@:+-]*$ ]]; then
    echo "$name contains characters that cannot be written safely to $ENV_FILE" >&2
    exit 2
  fi
}

write_agent_environment() {
  local app_service=$1 app_user=$2 app_config=$3 app_process=$4
  local tunnel_service=$5 static_forward_dir=$6 provider_gateway=$7
  for pair in \
    "SLSKDN_SERVICE_NAME:$app_service" \
    "SLSKDN_SERVICE_USER:$app_user" \
    "SLSKDN_PROCESS_NAME:$app_process" \
    "SLSKDN_CONFIG:$app_config" \
    "SLSKDN_VPN_TUNNEL_TYPE:$TUNNEL_TYPE" \
    "SLSKDN_VPN_IFACE:$VPN_IFACE" \
    "SLSKDN_VPN_TUNNEL_SERVICE:$tunnel_service" \
    "VPN_PORT_FORWARD_BACKEND:$BACKEND" \
    "SLSKDN_VPN_STATIC_FORWARD_DIR:$static_forward_dir" \
    "PF_GATEWAY:$provider_gateway"; do
    safe_env_value "${pair%%:*}" "${pair#*:}"
  done

  install -d -m 0755 "$(dirname "$ENV_FILE")"
  cat > "$ENV_FILE" <<EOF
SLSKDN_SERVICE_NAME=$app_service
SLSKDN_SERVICE_USER=$app_user
SLSKDN_PROCESS_NAME=$app_process
SLSKDN_CONFIG=$app_config
SLSKDN_VPN_TUNNEL_TYPE=$TUNNEL_TYPE
SLSKDN_VPN_IFACE=$VPN_IFACE
SLSKDN_VPN_TUNNEL_SERVICE=$tunnel_service
VPN_PORT_FORWARD_BACKEND=$BACKEND
SLSKDN_VPN_STATIC_FORWARD_DIR=$static_forward_dir
PF_GATEWAY=$provider_gateway
EOF
  chmod 0644 "$ENV_FILE"
}

install_systemd_unit() {
  local name=$1 app_service=$2 destination="/etc/systemd/system/$1"
  install_file 0644 "$ROOT/systemd/$name" "$destination"
  if [[ "$app_service" != "slskdN" ]]; then
    sed -i "/^\\[Install\\]/,\$! s/slskdN\\.service/${app_service}.service/g" "$destination"
  fi
}

install_external_tunnel_ordering() {
  [[ "$TUNNEL_TYPE" == "wireguard" || -z "$TUNNEL_SERVICE" ]] && return
  systemctl cat "$TUNNEL_SERVICE" >/dev/null 2>&1 || {
    echo "Systemd VPN service not found: $TUNNEL_SERVICE" >&2
    exit 2
  }

  local tunnel_unit=$TUNNEL_SERVICE unit dropin_dir
  [[ "$tunnel_unit" == *.* ]] || tunnel_unit+=".service"
  for unit in slskdN-vpn-split.service slskdN-vpn-ingress.service; do
    dropin_dir="/etc/systemd/system/${unit}.d"
    install -d -m 0755 "$dropin_dir"
    cat > "${dropin_dir}/tunnel.conf" <<EOF
[Unit]
Wants=$tunnel_unit
After=$tunnel_unit
EOF
  done
}

require_root

case "$MODE" in
  install|--install)
    ;;
  relay|--relay)
    for cmd in conntrack ip iptables systemctl tc; do
      require_command "$cmd"
    done
    install_agent_binary
    install_file 0644 "$ROOT/systemd/slskdN-relay.service" /etc/systemd/system/slskdN-relay.service
    install -d -m 0700 /etc/slskdN-relay
    install -d -m 0755 /var/lib/slskdN-vpn
    if [[ ! -e /etc/slskdN-relay/relay.env ]]; then
      install_file 0600 "$ROOT/examples/self-hosted-relay.env.example" /etc/slskdN-relay/relay.env
    fi
    systemctl daemon-reload
    echo "Relay companion installed but not started. Configure Tailscale or WireGuard, relay.env, and api-keys, then enable slskdN-relay.service."
    exit 0
    ;;
  check|--check|verify|--verify)
    if [[ -x "$ROOT/slskdN-vpn-agent" ]]; then
      "$ROOT/slskdN-vpn-agent" verify
    else
      require_command dotnet
      dotnet run --project "$ROOT/slskdN-vpn-agent.csproj" -- verify
    fi
    exit $?
    ;;
  *)
    usage >&2
    exit 64
    ;;
esac

BACKEND=${VPN_PORT_FORWARD_BACKEND:-natpmp}
TUNNEL_TYPE=${SLSKDN_VPN_TUNNEL_TYPE:-wireguard}
VPN_IFACE=${SLSKDN_VPN_IFACE:-slskdN-vpn}
TUNNEL_SERVICE=${SLSKDN_VPN_TUNNEL_SERVICE:-}
STATIC_FORWARD_DIR=${SLSKDN_VPN_STATIC_FORWARD_DIR:-/etc/slskdN-vpn/static-forwards}
PF_GATEWAY_VALUE=${PF_GATEWAY:-}

case "$BACKEND" in
  natpmp|static) ;;
  *) echo "Unsupported VPN_PORT_FORWARD_BACKEND: $BACKEND (use natpmp or static)" >&2; exit 2 ;;
esac

case "$TUNNEL_TYPE" in
  wireguard) TUNNEL_SERVICE=${TUNNEL_SERVICE:-"wg-quick@${VPN_IFACE}"} ;;
  openvpn|tailscale|other) ;;
  *) echo "Unsupported SLSKDN_VPN_TUNNEL_TYPE: $TUNNEL_TYPE" >&2; exit 2 ;;
esac

if [[ "$TUNNEL_TYPE" == "wireguard" ]]; then
  PF_GATEWAY_VALUE=${PF_GATEWAY_VALUE:-10.2.0.1}
elif [[ "$BACKEND" == "natpmp" && -z "$PF_GATEWAY_VALUE" ]]; then
  echo "Set PF_GATEWAY to the VPN provider's NAT-PMP gateway for an external tunnel." >&2
  exit 2
fi

if [[ "$TUNNEL_TYPE" != "wireguard" && -z "${SLSKDN_VPN_IFACE:-}" ]]; then
  echo "Set SLSKDN_VPN_IFACE to the active external VPN interface (for example tun0 or tailscale0)." >&2
  exit 2
fi

required=(jq curl ip iptables systemctl)
if [[ "$TUNNEL_TYPE" == "wireguard" ]]; then
  required+=(wg wg-quick)
fi
for cmd in "${required[@]}"; do
  require_command "$cmd"
done

if [[ "$BACKEND" == "natpmp" ]]; then
  require_command natpmpc
fi

if [[ "$TUNNEL_TYPE" == "wireguard" ]]; then
  wireguard_config="/etc/wireguard/${VPN_IFACE}.conf"
  [[ -s "$wireguard_config" ]] || {
    echo "Missing $wireguard_config. Copy your VPN provider's outbound WireGuard config there first." >&2
    exit 3
  }

  shopt -s nullglob
  ingress_configs=(/etc/wireguard/slskdN-vpn-ingress/*.conf)
  (( ${#ingress_configs[@]} > 0 )) || {
    echo "Missing /etc/wireguard/slskdN-vpn-ingress/*.conf. Add the provider config used for the forwarded Soulseek port first." >&2
    exit 4
  }
fi

if [[ "$BACKEND" == "static" ]]; then
  shopt -s nullglob
  static_forwards=("$STATIC_FORWARD_DIR"/pf*.env)
  (( ${#static_forwards[@]} > 0 )) || {
    echo "Missing static provider port files in $STATIC_FORWARD_DIR. See GETTING_STARTED.md." >&2
    exit 5
  }
fi

APP_SERVICE=$(resolve_app_service)
APP_USER=$(resolve_app_user "$APP_SERVICE")
if [[ "$APP_USER" == "root" ]]; then
  echo "The VPN agent needs a dedicated, non-root slskd service user; routing root would affect unrelated host traffic." >&2
  exit 6
fi
APP_CONFIG=${SLSKDN_CONFIG:-${SLSKD_CONFIG:-}}
if [[ -z "$APP_CONFIG" ]]; then
  if [[ -f /etc/slskd/slskd.yml ]]; then
    APP_CONFIG=/etc/slskd/slskd.yml
  else
    APP_CONFIG=/etc/slskdN/slskd.yml
  fi
fi
APP_PROCESS=${SLSKDN_PROCESS_NAME:-$APP_SERVICE}

install_agent_binary
write_agent_environment "$APP_SERVICE" "$APP_USER" "$APP_CONFIG" "$APP_PROCESS" "$TUNNEL_SERVICE" "$STATIC_FORWARD_DIR" "$PF_GATEWAY_VALUE"

units=(
  slskdN-vpn-split.service
  slskdN-vpn-ingress.service
  slskdN-vpn-ingress-renew.service
  slskdN-vpn-ingress-renew.timer
  slskdN-vpn-gluetun-compat.service
  slskdN-vpn-watchdog.service
  slskdN-vpn-watchdog.timer
  slskdN-relay.service
)
for unit in "${units[@]}"; do
  install_systemd_unit "$unit" "$APP_SERVICE"
done
install_external_tunnel_ordering

install -d -m 0755 /var/lib/slskdN-vpn
systemctl daemon-reload
if [[ "$TUNNEL_TYPE" == "wireguard" ]]; then
  systemctl enable --now "$TUNNEL_SERVICE"
fi
systemctl enable --now slskdN-vpn-split.service
systemctl enable --now slskdN-vpn-gluetun-compat.service
systemctl enable --now slskdN-vpn-ingress-renew.timer
systemctl enable --now slskdN-vpn-watchdog.timer
systemctl enable --now slskdN-vpn-ingress.service

echo "VPN agent installed and started for $APP_SERVICE.service (user $APP_USER)."
echo "Configuration saved to $ENV_FILE."
echo "Confirm integrations.vpn.enabled=true in $APP_CONFIG; if you changed it after setup, restart with: sudo systemctl restart $APP_SERVICE"
echo "Verify with: sudo $AGENT_LINK verify"
