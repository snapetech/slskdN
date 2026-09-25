# Administration

## Version

The package currently installs stable slskdN release
`2026092517-slskdn.325` on amd64 and arm64. Future stable GitHub releases are
tracked automatically by the package source updater.

## Configuration and data

The main configuration is __DATA_DIR__/slskd.yml. Downloads and incomplete
transfers are stored under the same YunoHost data directory. YunoHost backups
include that directory, so backups can grow with downloaded files.

Remote configuration is enabled and protected by slskdN's generated web
password. The Soulseek username and password are left unset on first install;
configure them after logging in. Shared directories are empty by default.

## Network

The YunoHost package selects and opens one incoming port for TCP and UDP. The
TCP listener carries Soulseek peer connections and the shared mesh listener.
The UDP listener carries DHT traffic. YunoHost opens the host firewall; routers
and upstream NAT may still need an explicit TCP and UDP port forward.

The peer listener intentionally binds to all network interfaces so other
Soulseek peers can connect directly. The web interface remains bound to
loopback behind YunoHost's reverse proxy.

The service does not enable UPnP. Configure any router forwarding yourself.

## Service and logs

The service name is the app ID, usually slskdn. View its status with
sudo yunohost service status slskdn. Logs are written to
/var/log/slskdn/slskdn.log and rotated weekly.
