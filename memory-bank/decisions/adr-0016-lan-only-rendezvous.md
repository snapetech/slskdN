# ADR-0016: LAN-only mesh bypasses the public DHT engine

## Context

LAN-only radio validation logged public DHT nodes and peer discovery despite a
startup warning promising no public bootstrap. Inspection of the installed
MonoTorrent assembly proved that an empty bootstrap-router array selects the
library's public defaults. Its static bootstrap-node cache also makes supplying
a dummy local router an unreliable isolation boundary.

## Decision

When LanOnly is enabled, do not construct or start the public BitTorrent DHT
engine and do not load saved public node tables. Start the shared UDP listener
independently when overlay/QUIC port sharing needs it. Keep known-peer overlay
connections, discovery outside public BitTorrent DHT and connection maintenance.
Guard explicit announce/discovery requests and DHT peer callbacks with the same
mode. Ordinary public-mode startup and defaults remain unchanged.

## Consequences

LAN-only status reports zero public DHT nodes and IsDhtRunning=false while mesh
transport can remain active. Public bootstrap timing is unnecessary in this
mode. Shared UDP routing still serves overlay/QUIC; DHT datagrams have no public
engine subscriber. Tests must verify actual startup, both node statuses and
bidirectional transport, rather than trusting an isolation warning. LAN-only
limits public DHT discovery; explicitly configured known remote peers remain
under operator control.

### Shutdown ownership

Cancel and await the owned initialization task before detaching engine/listener
resources and clearing startup/beacon state. This prevents a late ownership
assignment from escaping an early cleanup snapshot. Recheck cancellation after
overlay startup; startup timestamps cannot outlive a completed Stop.
