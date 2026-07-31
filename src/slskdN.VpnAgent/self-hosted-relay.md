# Self-Hosted Relay

Use this mode when slskdN and its files must remain on a home server behind
CGNAT while a small public VPS provides the Soulseek-facing address. The VPS is
a narrow network relay, not a second slskdN instance: it receives no share
paths, database, slskdN API key, configuration, or filesystem access.

## Traffic and Trust Boundary

```text
Soulseek peer <-> VPS public TCP port <-> WireGuard <-> home slskdN listener
                                      ^
home slskdN outbound Soulseek traffic -+

Home LAN <-> slskdN Web UI/API (never forwarded by the relay)
```

The relay permits one configured inbound TCP port and egress from one private
WireGuard CIDR. Its status API binds to the WireGuard address and requires an
independent API key. It is not a SOCKS, HTTP CONNECT, or arbitrary public TCP
proxy. The home host routes only the slskdN service UID through WireGuard, with
a blackhole fallback when the tunnel route disappears.

## Prerequisites

- Linux VPS with a public IPv4 address and an open UDP WireGuard port.
- Linux home host running slskdN as a dedicated service user.
- `wireguard-tools`, `iptables`, `iproute2`, `conntrack`, `iputils`, and
  optionally `tc` on the VPS.
- A VPS provider that permits IP forwarding and does not block the selected
  public TCP port.

The examples use `10.77.0.1` for the VPS, `10.77.0.2` for home, UDP `51820` for
WireGuard, and TCP `50300` for Soulseek. Change them consistently if they
conflict with an existing network.

## 1. Generate WireGuard and API Keys

Run once on each host and exchange only public WireGuard keys:

```bash
sudo install -d -m 700 /etc/wireguard
umask 077
wg genkey | sudo tee /etc/wireguard/slskdN-relay.key | wg pubkey | sudo tee /etc/wireguard/slskdN-relay.pub
```

Generate the relay status key on the VPS:

```bash
sudo install -d -m 700 /etc/slskdN-relay
openssl rand -base64 48 | sudo tee /etc/slskdN-relay/api-keys >/dev/null
sudo chmod 600 /etc/slskdN-relay/api-keys
```

Copy that status key to a root-readable file on the home host. Do not reuse a
slskdN API key or a WireGuard private key.

## 2. Configure WireGuard on the VPS

Create `/etc/wireguard/slskdN-relay.conf`:

```ini
[Interface]
Address = 10.77.0.1/30
ListenPort = 51820
PrivateKey = VPS_PRIVATE_KEY

[Peer]
PublicKey = HOME_PUBLIC_KEY
AllowedIPs = 10.77.0.2/32
```

Enable it:

```bash
sudo systemctl enable --now wg-quick@slskdN-relay
```

## 3. Configure WireGuard on the Home Host

Create `/etc/wireguard/slskdN-relay.conf`:

```ini
[Interface]
Address = 10.77.0.2/30
PrivateKey = HOME_PRIVATE_KEY
Table = off

[Peer]
PublicKey = VPS_PUBLIC_KEY
Endpoint = VPS_PUBLIC_IP:51820
AllowedIPs = 0.0.0.0/0
PersistentKeepalive = 25
```

`Table = off` is required. The VPN agent creates a separate policy-routing
table for the slskdN service UID; it does not replace the home host's default
route. `AllowedIPs = 0.0.0.0/0` lets outbound Soulseek connections use the VPS.

## 4. Install and Start the VPS Companion

Install the released `slskdN-vpn-agent` binary at
`/usr/local/bin/slskdN-vpn-agent`, then copy:

- `examples/self-hosted-relay.env.example` to `/etc/slskdN-relay/relay.env`
- `systemd/slskdN-relay.service` to `/etc/systemd/system/`

Edit the environment file for the actual public interface and ports. The
defaults cap the tunnel at 100 Mbit/s and 128 concurrent forwarded
connections. Setting the bandwidth value to `0` disables shaping; increasing
either limit increases VPS and Soulseek-network impact.

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now slskdN-relay
sudo systemctl status slskdN-relay
```

The companion discovers its public IPv4 address, installs idempotent DNAT,
forwarding, connection-limit, and masquerade rules, then exposes its status API
only at `http://10.77.0.1:8010`. Set `SLSKDN_RELAY_PUBLIC_IP` when outbound
address discovery is unavailable.

### Container Alternative

`Dockerfile.relay` and `compose.relay.yml` package the same companion. It needs
host networking and `NET_ADMIN` because it configures the host network
namespace. Start WireGuard on the VPS host first, then run:

```bash
docker compose -f compose.relay.yml up -d --build
```

The native systemd service is the recommended deployment because its network
privileges and startup ordering are easier to audit.

## 5. Configure the Home VPN Agent

Configure the home tunnel as an externally managed WireGuard interface. A
systemd drop-in for `slskdN-vpn-split.service` should contain:

```ini
[Unit]
After=wg-quick@slskdN-relay.service
Requires=wg-quick@slskdN-relay.service

[Service]
Environment=SLSKDN_VPN_TUNNEL_TYPE=external-wireguard
Environment=SLSKDN_VPN_IFACE=slskdN-relay
Environment=SLSKDN_VPN_TUNNEL_SERVICE=wg-quick@slskdN-relay
Environment=SLSKDN_VPN_TABLE=51820
```

The split-routing service installs a default route through `slskdN-relay` plus
a higher-metric blackhole default in table `51820`. If WireGuard goes down,
slskdN cannot silently fall back to the home ISP. Local UI/API and LAN traffic
continue to use the main routing table.

## 6. Configure slskdN

Use the relay's authenticated, WireGuard-only status endpoint:

```yaml
soulseek:
  listen_port: 50300

integrations:
  vpn:
    enabled: true
    port_forwarding: true
    self_hosted_relay: true
    polling_interval: 5000
    gluetun:
      url: http://10.77.0.1:8010
      timeout: 5000
      api_key: RELAY_STATUS_API_KEY
```

The relay implements the narrow Gluetun-compatible endpoints already consumed
by slskdN, plus `/v1/slskdn/relay`. slskdN automatically obtains the VPS public
IP and advertised TCP port. System -> Integrations -> VPN shows tunnel health,
latency, transferred bytes, active/maximum connections, bandwidth policy,
latest handshake, and the advertised endpoint.

The Web UI/API stays on its existing home bind address. Do not add ports `5030`
or `5031` to the relay configuration or VPS firewall.

## 7. Verify Fail-Closed Behavior

```bash
sudo /usr/local/bin/slskdN-vpn-agent verify
curl -H "X-API-Key: $(sudo head -n1 /etc/slskdN-relay/api-keys)" http://10.77.0.1:8010/v1/slskdn/relay
sudo -u slskd curl -4 https://ifconfig.me/ip
```

The last command must return the VPS public IP. Stop WireGuard briefly and
repeat it; the request must fail rather than return the home public IP. Restore
WireGuard immediately after the test.

## Authentication and Key Rotation

The relay reads at most two non-empty keys from `api-keys`, allowing an overlap
window without accepting an unbounded key set:

1. Add a new key as the first line and retain the old key as the second line.
2. Reload or restart `slskdN-relay`.
3. Update `integrations.vpn.gluetun.api_key` on the home instance and restart
   slskdN.
4. Confirm the VPN panel is connected and current.
5. Remove the old second line and restart the relay.

Rotate WireGuard independently by adding a second peer during the overlap,
moving the home configuration to the new key, confirming a recent handshake,
and only then removing the old peer. Never replace both WireGuard peers at once.

## Operational Boundaries

- All uploads and downloads consume VPS transfer allowance.
- Only the configured TCP port is accepted from the public interface.
- The status API binds to the WireGuard address and rejects requests without a
  current or previous API key using constant-time comparison.
- The VPS contains no slskdN API key and never calls the home API.
- The companion does not expose filesystem, configuration, management, SOCKS,
  HTTP proxy, or arbitrary destination APIs.
- Keep conservative defaults and increase limits only after observing actual
  usage and VPS capacity.
