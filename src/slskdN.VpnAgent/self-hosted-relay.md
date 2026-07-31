# Self-Hosted Relay

Use this mode when slskdN and its files must remain on a home server behind
CGNAT while a small public VPS provides the Soulseek-facing address. The VPS is
a narrow network relay, not a second slskdN instance: it receives no share
paths, database, slskdN API key, configuration, or filesystem access.

## Traffic and Trust Boundary

```text
Soulseek peer <-> VPS public TCP port <-> Tailscale/WireGuard <-> home slskdN listener
                                                ^
home slskdN outbound Soulseek traffic ---------+

Home LAN <-> slskdN Web UI/API (never forwarded by the relay)
```

The relay permits one configured inbound TCP port and egress from exactly one
home tunnel address. Its status API binds to the private tunnel address and
requires an independent API key. It is not a SOCKS, HTTP CONNECT, or arbitrary
public TCP proxy.

Tailscale is the recommended transport when it already exists on both hosts.
Do not create a second WireGuard tunnel: Tailscale already supplies the
WireGuard data plane, NAT traversal, peer identity, and optional DERP fallback.
The companion adds only the bounded public-port forwarding and status surface
that Tailscale does not provide.

## Prerequisites

- Linux VPS with a public IPv4 address and Tailscale or WireGuard connectivity.
- Linux home host running slskdN as a dedicated service user.
- `tailscale`, `iptables`, `iproute2`, `conntrack`, and optionally `tc` on the
  VPS. Use `wireguard-tools` and `iputils` for the alternate WireGuard path.
- A VPS provider that permits IP forwarding and does not block the selected
  public TCP port.

The examples use `100.64.0.1` for the VPS Tailscale address, `100.64.0.2` for
home, and TCP `50300` for Soulseek. Substitute the actual stable tailnet
addresses reported by `tailscale ip -4`.

## Recommended: Tailscale

### 1. Prepare the OCI Relay

Join the OCI host and home host to the same tailnet. On OCI, advertise it as an
exit node and approve the route in the Tailscale admin console:

```bash
sudo tailscale set --advertise-exit-node
tailscale ip -4
```

Record the OCI and home Tailscale IPv4 addresses. Confirm that OCI can reach
the home address with `tailscale ping HOME_TAILSCALE_IP`. The companion accepts
only the exact configured home `/32`; it does not accept the entire tailnet.

Allow TCP `50300` in the OCI security list/NSG and host firewall. Tailscale
manages its own encrypted peer connectivity; no additional WireGuard UDP port
or key exchange is needed.

### 2. Install the OCI Companion

Install the released `slskdN-vpn-agent` binary at
`/usr/local/bin/slskdN-vpn-agent`, then copy:

- `examples/self-hosted-relay-tailscale.env.example` to
  `/etc/slskdN-relay/relay.env`
- `systemd/slskdN-relay.service` to `/etc/systemd/system/`

Set `SLSKDN_RELAY_HOME_IP` to the home node's Tailscale IPv4 address and set
the public interface if it is not `eth0`. Leave `SLSKDN_RELAY_API_HOST` unset;
the companion discovers OCI's Tailscale IPv4 address. Add this systemd drop-in:

```ini
# /etc/systemd/system/slskdN-relay.service.d/tailscale.conf
[Unit]
After=tailscaled.service
Requires=tailscaled.service
```

Generate the independent relay status key and start the companion:

```bash
sudo install -d -m 700 /etc/slskdN-relay
openssl rand -base64 48 | sudo tee /etc/slskdN-relay/api-keys >/dev/null
sudo chmod 600 /etc/slskdN-relay/api-keys
sudo systemctl daemon-reload
sudo systemctl enable --now slskdN-relay
```

The service validates `tailscale0`, confirms the home peer is present, installs
idempotent DNAT/forward/connection-limit/masquerade rules, and exposes its API
only on OCI's tailnet address. `tailscale status --json` and a bounded
`tailscale ping` provide peer state, counters, latency, and direct/DERP path.

### 3. Route Only the Home slskdN Container

Do not select the OCI exit node on the home host if other home traffic must keep
using the normal ISP route. Put Tailscale in a Docker sidecar and make only the
slskdN container share that sidecar's network namespace:

```yaml
services:
  tailscale-slskdn:
    image: tailscale/tailscale:stable
    hostname: slskdn-home
    environment:
      TS_STATE_DIR: /var/lib/tailscale
    volumes:
      - tailscale-state:/var/lib/tailscale
      - /dev/net/tun:/dev/net/tun
    cap_add:
      - NET_ADMIN
      - SYS_MODULE

  slskdn:
    image: ghcr.io/snapetech/slskdn:latest
    network_mode: service:tailscale-slskdn
    depends_on:
      - tailscale-slskdn
    # Keep the existing slskdN volumes and environment here.

volumes:
  tailscale-state:
```

Authenticate the sidecar, select OCI as its exit node, and publish the slskdN
Web UI on the `tailscale-slskdn` service (the service owning the shared network
namespace), not on `slskdn`:

```bash
docker compose exec tailscale-slskdn tailscale up
docker compose exec tailscale-slskdn tailscale set --exit-node=OCI_TAILSCALE_IP
```

Add the existing Web UI port mapping to `tailscale-slskdn`, for example
`127.0.0.1:5030:5030`, and reach it through the host or an existing reverse
proxy. Do not publish the Soulseek listener on the home router; OCI forwards it
over Tailscale. This sidecar arrangement makes uploads, downloads, and Soulseek
control traffic use OCI without changing the route for unrelated containers or
the home host.

### 4. Configure slskdN

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
      url: http://OCI_TAILSCALE_IP:8010
      timeout: 5000
      api_key: RELAY_STATUS_API_KEY
```

The VPN panel reports `tailscale`, peer activity, latency, byte counters, and
the current direct or DERP path. The status endpoint becoming unreachable or
the home peer going offline causes slskdN to treat the public endpoint as
disconnected.

### 5. Verify

```bash
curl -H "X-API-Key: RELAY_STATUS_API_KEY" http://OCI_TAILSCALE_IP:8010/v1/slskdn/relay
docker compose exec slskdn curl -4 https://ifconfig.me/ip
```

The second command must return the OCI public IP. Stop the Tailscale sidecar
briefly and confirm that the slskdN container cannot fall back to the home ISP.

## Alternate: Dedicated WireGuard Tunnel

Use this path only when Tailscale is unavailable or an independently managed
tunnel is specifically required. The examples use `10.77.0.1` for the VPS,
`10.77.0.2` for home, UDP `51820`, and TCP `50300`.

### 1. Generate WireGuard and API Keys

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

### 2. Configure WireGuard on the VPS

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

### 3. Configure WireGuard on the Home Host

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

### 4. Install and Start the VPS Companion

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

### 5. Configure the Home VPN Agent

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

### 6. Configure slskdN

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

### 7. Verify Fail-Closed Behavior

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

For the alternate transport, rotate WireGuard independently by adding a second
peer during the overlap, moving the home configuration to the new key,
confirming a recent handshake, and only then removing the old peer. Never
replace both WireGuard peers at once. Tailscale node-key rotation remains owned
by Tailscale.

## Operational Boundaries

- All uploads and downloads consume VPS transfer allowance.
- Only the configured TCP port is accepted from the public interface.
- The status API binds to the private tunnel address and rejects requests
  without a current or previous API key using constant-time comparison.
- The VPS contains no slskdN API key and never calls the home API.
- The companion does not expose filesystem, configuration, management, SOCKS,
  HTTP proxy, or arbitrary destination APIs.
- Keep conservative defaults and increase limits only after observing actual
  usage and VPS capacity.
