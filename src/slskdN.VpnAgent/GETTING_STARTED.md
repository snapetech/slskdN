# Start Here: slskdN VPN Agent

The VPN agent is a native helper that keeps slskdN Soulseek traffic on a VPN
interface and can publish a provider-forwarded Soulseek port. It is included in
the release ZIP under `vpn-agent/`; it does not run in Wine and does not need
the .NET SDK when installed from a release.

The agent does not sign in to your VPN provider or start a Windows/macOS VPN
client. On Linux it can start a WireGuard tunnel from a config you provide. On
Windows and macOS, connect the VPN client yourself first. Port forwarding also
depends on your provider: dynamic NAT-PMP claiming is a Linux feature, while
Windows/macOS use a port your provider assigned or forwards for you.

## Linux release install

The Linux release installer places the app in `/opt/slskdn`, installs the
native helper and systemd units, and leaves the VPN agent stopped until you
configure it. The helper is not active just because it was installed.

1. Set up your VPN provider and choose one of the supported modes below.
2. Edit `/etc/slskd/slskd.yml` and enable the VPN integration. Add this under
   the existing `integrations:` key, or merge these keys into its existing
   `vpn:` section:

   ```yaml
   integrations:
     vpn:
       enabled: true
       port_forwarding: true
       gluetun:
         url: http://127.0.0.1:8010
         timeout: 5000
   ```

3. Run the installer as root. It uses the helper already in the release ZIP;
   no source checkout or .NET SDK is needed:

   ```bash
   sudo /opt/slskdn/vpn-agent/install.sh
   ```

4. Check the services and agent status:

   ```bash
   sudo systemctl status slskdN-vpn-split slskdN-vpn-gluetun-compat slskdN-vpn-ingress
   sudo /usr/local/bin/slskdN-vpn-agent status
   ```

   The raw Linux release installer names the app service `slskd`; distro
   packages may name it `slskdN`. The agent detects and wires up either name.
   The app service starts after the VPN routing service is active.

The setup keeps the Web UI on its normal local address, usually
`http://localhost:5030`. It routes the service user's Soulseek traffic through
the VPN. HTTPS at port 5031 is available only if you configure it separately.

### Linux with WireGuard

This is the full host-managed mode. Install the WireGuard tools, `jq`, `curl`,
`iproute2`, `iptables`, and the NAT-PMP client if your provider uses NAT-PMP.
Package names vary by distribution. The release installer already installs the
.NET runtime needed by slskdN; the VPN agent itself is self-contained.

Copy the provider's outbound WireGuard config to
`/etc/wireguard/slskdN-vpn.conf`. For each incoming provider tunnel/forward,
copy its separate WireGuard config into
`/etc/wireguard/slskdN-vpn-ingress/` with a `.conf` suffix. Do not reuse a
private key for simultaneous outbound and ingress tunnels. Protect the files:

```bash
sudo install -d -m 700 /etc/wireguard/slskdN-vpn-ingress
sudo install -m 600 /path/to/provider-outbound.conf /etc/wireguard/slskdN-vpn.conf
sudo install -m 600 /path/to/provider-ingress.conf \
  /etc/wireguard/slskdN-vpn-ingress/00-soulseek.conf
```

For a provider with dynamic NAT-PMP forwarding, run the installer as shown
above. The default backend is NAT-PMP. Your provider must support it and its
gateway must be reachable through the tunnel. If the gateway is not
`10.2.0.1`, pass your provider's NAT-PMP gateway to the installer. It saves
the value in `/etc/default/slskdN-vpn-agent` before it starts the services.

Replace this example address with your provider's gateway:

```bash
sudo env PF_GATEWAY=10.10.0.1 /opt/slskdn/vpn-agent/install.sh
```

For a provider-assigned static forwarded port, make a file for the Soulseek TCP
forward. Use the actual public IP, public port, and local listener port from
your provider and slskdN configuration:

```bash
sudo install -d -m 700 /etc/slskdN-vpn/static-forwards
sudo tee /etc/slskdN-vpn/static-forwards/pf0.env >/dev/null <<'EOF'
public_port=51000
public_ip=YOUR_PROVIDER_PUBLIC_IP
local_port=50300
proto=tcp
EOF
```

Replace the example values. Then select the static backend:

```bash
sudo env VPN_PORT_FORWARD_BACKEND=static \
  /opt/slskdn/vpn-agent/install.sh
```

The `local_port` must match `soulseek.listen_port`; `public_port` is the port
assigned by the provider. If you also use public DHT/mesh UDP, configure its
provider forward as the next `pfN.env` slot. The advanced WireGuard setup is in
[`manual-linux-wireguard.md`](manual-linux-wireguard.md).

### Linux with an existing OpenVPN, Tailscale, or other tunnel

Connect the provider tunnel first and confirm its interface exists. Examples
are `tun0` for OpenVPN and `tailscale0` for Tailscale. The Linux agent does not
log in to that provider client. Use a static provider forward unless the
provider offers a compatible NAT-PMP service.

For example, with an OpenVPN interface and a static port file already in place:

```bash
sudo env \
  SLSKDN_VPN_TUNNEL_TYPE=openvpn \
  SLSKDN_VPN_IFACE=tun0 \
  SLSKDN_VPN_TUNNEL_SERVICE=openvpn-client@provider \
  VPN_PORT_FORWARD_BACKEND=static \
  /opt/slskdn/vpn-agent/install.sh
```

Replace the interface and service with the names used on your system. The
installer saves these values in `/etc/default/slskdN-vpn-agent` so the systemd
services use them after reboot. The external-tunnel guide has more examples:
[`external-tunnel.md`](external-tunnel.md).

## Windows

The Windows release ZIP includes `vpn-agent/slskdN-vpn-agent.exe`. Connect your
VPN client, extract the slskdN release ZIP, and open PowerShell **as
Administrator**. Find the VPN adapter's exact name with `Get-NetAdapter`, then
run:

```powershell
Get-NetAdapter | Where-Object Status -eq 'Up' | Select-Object Name, InterfaceDescription
$env:SLSKDN_APP_PATH = 'C:\path\to\slskd.exe'
$env:SLSKDN_VPN_IFACE = 'VPN adapter name'
.\vpn-agent\slskdN-vpn-agent.exe platform-split
```

Replace both example values with the installed app path and adapter name. The
helper adds Windows Defender Firewall rules that block the slskdN executable
on other active network adapters. Run it again if the executable path or VPN
adapter name changes. It does not start the VPN client or claim dynamic
forwarded ports; configure a provider-assigned forwarded port separately.

## macOS

The macOS release ZIP includes `vpn-agent/slskdN-vpn-agent`. Connect your VPN
client, extract the slskdN release ZIP, and use Terminal to identify the
active VPN interface (commonly `utunN`) with `ifconfig`. Then run the helper as
root, replacing `utun4` with the interface you found:

```bash
ifconfig | grep '^utun'
sudo env SLSKDN_SERVICE_USER="$(id -un)" SLSKDN_VPN_IFACE=utun4 \
  ./vpn-agent/slskdN-vpn-agent platform-split
```

Use the account that runs slskdN for `SLSKDN_SERVICE_USER`. The helper adds a
macOS `pf` rule that lets that account send traffic only through the named VPN
interface, while preserving loopback. It backs up `/etc/pf.conf` before adding
its anchor. If your VPN reconnects using a different `utunN` interface, rerun
the helper with the new interface. It does not start the VPN client or claim
dynamic forwarded ports; configure a provider-assigned forwarded port
separately.

## If a command fails

- `No packaged agent binary or source project found`: use the release's Linux
  `vpn-agent/install.sh` from inside `/opt/slskdn/vpn-agent`; do not copy only
  the script by itself.
- `Missing ...` command: install the named Linux package for your distribution.
- `VPN interface not found`: connect the VPN first and use the current
  interface name.
- `No ingress VPN configs found`: in WireGuard mode, add at least one separate
  provider ingress config. In external-tunnel mode, check that the configured
  static forward file exists and matches the listener.
- `No slskdN listener ports found`: check the service configuration and status,
  then run the installer again after the app has started.

For the full command reference and troubleshooting notes, see
[`README.md`](README.md) and [`windows-macos.md`](windows-macos.md).
