# ADR-0030: LAN Discovery Advertising Opt-In

**Status**: Accepted
**Date**: 2026-10-04

## Context

`feature.IdentityFriends` controls profile, contact, invite, and nearby-peer
APIs, and it defaults to enabled. Startup also used that broad API feature to
start mDNS advertising. Advertising publishes the peer ID, friend code,
display name, API port, and capabilities to the local network, so users could
not keep Identity/Friends APIs available without also publishing that data.
Metrics and tracing already have independent, default-off controls.

## Decision

Add a separate `lan_discovery.advertise` option that defaults to `false` and
requires an application restart. Startup advertising requires both
`feature.IdentityFriends` and `lan_discovery.advertise`. Keep nearby-peer
browsing available as an explicit user-triggered API operation whenever the
Identity/Friends APIs are enabled.

## Consequences

Automatic multicast publication is opt-in. Existing users who rely on
startup advertising must set `lan_discovery.advertise: true`. API availability
and manual nearby-peer browsing are unchanged. The new option is documented in
the shipped configuration example and release notes.
