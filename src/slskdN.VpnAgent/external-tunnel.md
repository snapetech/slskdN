# External Tunnel Setup

Use this path when OpenVPN, Tailscale, a provider desktop client, or another
service already owns the VPN tunnel interface. The slskdN VPN agent still
applies fail-closed routing, exposes forwarded-port state, and can reconcile
ingress, but it does not log in to the tunnel provider. For a released ZIP
install, configure the static forward file and run the bundled `install.sh` as
shown in [`GETTING_STARTED.md`](GETTING_STARTED.md). The manual systemd examples
below are for an operator managing the units directly.

## Supported Inputs

- `SLSKDN_VPN_TUNNEL_TYPE`: `openvpn`, `tailscale`, or another descriptive
  external tunnel type.
- `SLSKDN_VPN_IFACE`: the interface name, such as `tun0` or `tailscale0`.
- `SLSKDN_VPN_TUNNEL_SERVICE`: optional systemd unit used for startup ordering
  and health checks.
- `VPN_PORT_FORWARD_BACKEND`: usually `static` for external tunnels.
- `SLSKDN_VPN_STATIC_FORWARD_DIR`: directory containing provider forward files.

## Static Forward Files

Create one file per provider mapping before running the installer. Use the
provider's actual public endpoint and port, and set `local_port` to the
configured `soulseek.listen_port`:

```bash
sudo install -d -m 700 /etc/slskdN-vpn/static-forwards
sudo tee /etc/slskdN-vpn/static-forwards/pf0.env >/dev/null <<'EOF'
public_port=51000
public_ip=YOUR_PROVIDER_PUBLIC_IP
local_port=50300
proto=tcp
EOF
```

`pf0` is the Soulseek TCP mapping. If you expose public DHT/mesh/QUIC over UDP,
add the next provider mapping as `pf1.env` with `proto=udp` and the correct
local listener port. Replace the example values before starting the agent.

## OpenVPN Example

Start the provider-managed client first:

```bash
systemctl is-active openvpn-client@provider
ip link show tun0
```

For the release installer, configure the agent and make systemd start the
provider service before applying VPN routing:

```bash
sudo env SLSKDN_VPN_TUNNEL_TYPE=openvpn \
  SLSKDN_VPN_IFACE=tun0 \
  SLSKDN_VPN_TUNNEL_SERVICE=openvpn-client@provider \
  VPN_PORT_FORWARD_BACKEND=static \
  /opt/slskdn/vpn-agent/install.sh
```

## Tailscale Example

Tailscale usually does not provide generic public VPN port forwarding. Use this
mode for private tailnet routing or custom exit-node setups where you provide a
static/manual forwarded-port state.

```bash
sudo env SLSKDN_VPN_TUNNEL_TYPE=tailscale \
  SLSKDN_VPN_IFACE=tailscale0 \
  SLSKDN_VPN_TUNNEL_SERVICE=tailscaled \
  VPN_PORT_FORWARD_BACKEND=static \
  /opt/slskdn/vpn-agent/install.sh
```

## Apply Changes

```bash
sudo systemctl daemon-reload
sudo systemctl restart slskdN-vpn-split.service
sudo systemctl restart slskdN-vpn-gluetun-compat.service
sudo systemctl restart slskdN-vpn-ingress.service
sudo systemctl restart slskdN-vpn-ingress-renew.timer
sudo /usr/local/bin/slskdN-vpn-agent verify
```

If the provider does not support inbound port forwarding, outbound Soulseek
traffic can still be forced through the VPN, but inbound reachability will be
limited.
