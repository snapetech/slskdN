# VPN Agent API Contract

The agent exposes a small local API so slskdN can learn forwarded-port state
without depending on a full Gluetun deployment.

## Compatibility Endpoints

The Gluetun-compatible service reads `/var/lib/slskdN-vpn/pf0.env` and exposes:

- `GET /v1/openvpn/portforwarded`
- `GET /v1/openvpn/status`

Older slskdN builds consume the `pf0` Soulseek mapping through these endpoints.

## slskdN Port-Forward Summary

Newer integration code can read all claimed mappings from:

- `GET /v1/slskdn/portforwards`

This endpoint returns the current forwarded-port slots discovered or written by
the ingress backend. It is intended for consolidated listener setups where more
than one protocol mapping may be present.

## Runtime State Files

- `/var/lib/slskdN-vpn/pfN.env`: one public mapping per slot.
- `/var/lib/slskdN-vpn/summary.env`: aggregate claim status.
- `/var/lib/slskdN-vpn/watchdog.failures`: watchdog failure counter.

Static providers can write equivalent `pfN.env` files under the configured
static-forward directory; the agent copies or consumes them according to the
selected backend.

## Commands

```bash
slskdN-vpn-agent <command>
```

- `api`: serve compatibility and slskdN-specific status endpoints.
- `ingress`: discover slskdN listener ports, claim/write forwarded-port state,
  and reconcile ingress rules.
- `cleanup-ingress`: remove ingress namespaces, veth links, rules, and route
  tables.
- `split`: configure Linux UID policy routing and fail-closed table.
- `platform-split`: configure Windows/macOS native enforcement or Linux UID
  policy routing.
- `verify`: run the full health check.
- `status`: print a human status check.
- `watchdog`: run one verification pass and recover ingress after repeated
  failures.
- `relay-apply`: install the bounded VPS forwarding, egress, and limit rules.
- `relay-api`: serve the authenticated Tailscale/WireGuard relay status API.
- `relay-run`: apply relay networking and then serve the status API.

## Self-Hosted Relay Extension

In self-hosted relay mode the compatibility surface binds to the VPS tunnel
address instead of loopback. Every request requires `X-API-Key` or a Bearer
token matching one of the two bounded keys in `SLSKDN_RELAY_API_KEY_FILE`. A
stale WireGuard handshake, or an offline/unreachable Tailscale peer, makes
`/v1/publicip/ip` return an empty address so slskdN treats the relay as
disconnected while retaining diagnostics.

`GET /v1/slskdn/relay` returns current tunnel liveness, transport, public and
target ports, round-trip latency, tunnel byte counters, active and maximum
connections, bandwidth policy, latest peer activity, and the observed path.
For WireGuard, latest peer activity is its latest handshake. For Tailscale, the
path is the bounded `tailscale ping` result and may identify a direct or DERP
route. The endpoint is read-only. The companion exposes no filesystem,
configuration, slskdN credential, arbitrary destination, SOCKS, or HTTP proxy
API.

## Current Port-Reduction Boundary

`core` and `compact` ingress modes reduce forwarded-port count, but they are not
protocol multiplexers. A true single-public-pair mode requires deeper slskdN
listener ownership changes so one TCP owner and one UDP owner can demultiplex
traffic before dispatching it internally.
