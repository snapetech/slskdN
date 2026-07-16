# System Admin Surfaces

This guide maps the current System area to the operator work it supports.
Advanced features should be controlled from these guided surfaces where
possible, with raw YAML left as the escape hatch.

The System tab menu labels admin and experimental panels directly. Keep those
labels current when a panel changes maturity or starts exposing privileged
actions.

## Start Here

- **System -> Info**: daemon status, version, pending restart/reconnect signals,
  setup health, and diagnostic bundles.
- **System -> Options**: raw YAML view/edit when remote configuration is enabled.
- **System -> Logs / Events / Metrics / Jobs**: operational history and current
  background work.

## Guided Flow Map

Use these current surfaces for the G5 guided-flow tracks before adding another
page:

- **Network Health**: System -> Network gives a local readiness score,
  connectivity diagnostics, DHT exposure notices, and a copyable report without
  contacting peers.
- **Improve Downloads**: System -> Swarm Analytics, System -> Source Providers,
  and Transfers expose multisource/rescue posture, provider priority, and active
  transfer state. Keep live acquisition actions explicit.
- **Discover Music**: Search, Wishlist, Discovery Graph, System -> Source
  Providers, and System -> MediaCore cover manual discovery, background search,
  provider capability review, and pod/native discovery checks.
- **Share Collection**: Collections and System -> Shares cover collection share
  actions and local share-scan posture; System -> Library Health covers
  review-first repair/export work.
- **Play Local Files**: the persistent player, collection play actions, and
  System -> Library Health expose local playback and library readiness.
- **Join Listening Party**: Messages/pod routes and the persistent player cover
  current listen-along entry points; keep new broadcast features opt-in and
  privacy-reviewed.

## Policies

**System -> Policies** is the guided YAML editor for operator settings that used
to be easy to miss.

It covers:

- Webhooks and scripts: names, events, target URL/command, timeout, retry count,
  and certificate-error posture.
- Transfer policy: upload/download slots, speed ceilings, direct retry policy,
  auto-replace, and scheduled-limit enablement.
- Security and access: auth disablement, explicit no-auth CIDRs, JWT key/TTL,
  API keys, HTTPS certificate settings, force HTTPS, and HTTP rate limits.
- Search/network policy: incoming search filters, search throttles, managed
  blacklist, DHT enablement, LAN-only mode, bootstrap routers, Scene Pod Bridge,
  and rescue mode.
- Retention/storage: search/event/log retention, transfer/file history retention,
  share cache workers/retention, and optional share media-attribute probing.

The panel writes YAML only. It does not test hooks, run scripts, contact peers,
restart the daemon, mutate transfers, validate provider credentials, or modify
files.

## Experience

**System -> Experience** stores browser-local preferences that page-specific
surfaces can consume.

It covers:

- Search ranking profile, preferred condition, duplicate folding, and action
  preview density.
- Discovery filters, confidence floor, stale age, and evidence-detail
  preference.
- Player queue, radio, ratings, history, scrobble, visualizer, and keyboard
  posture.
- Messages dense mode, pinned restore, unread badges, user filtering, and local
  search preference.

These preferences are stored in browser `localStorage`. Saving them does not
change daemon configuration or execute any network/file action.

## Integrations

**System -> Integrations** groups provider and destination setup:

- VPN status and port-forwarding visibility.
- Lidarr status, wanted sync, path mapping, and safe manual-import handoff.
- Metadata provider settings for Chromaprint, AcoustID, MusicBrainz, and Lidarr.
- Notification providers: Pushbullet, Ntfy, and Pushover.
- Source-feed imports: Spotify, YouTube, and Last.fm.
- FTP completed-download upload settings.
- Servarr readiness and media-server execution contracts for Plex,
  Jellyfin/Emby, and Navidrome.
- Federation diagnostics for ActivityPub and pod-signing posture.

Runtime apply actions are explicit. Provider credential checks and import/sync
actions should remain user-triggered and visibly rate-limited.

See [Federation Diagnostics](federation-diagnostics.md) for the read-only checks
and privacy posture.

## Source Providers

**System -> Source Providers** is a read-only acquisition provider catalog. It
shows provider registration, active/disabled state, risk level, capabilities,
network policy, disabled reasons, and acquisition-profile priority chains.

The catalog is observational. It does not start searches, browse peers, probe
DHT, download files, or validate credentials.

## Automation Center

**System -> Automations** lists automation recipes, local enablement toggles,
impact labels, cadence, and dry-run history. It is the right place to review
automation posture before enabling live backend execution.

## Security

**System -> Security** shows runtime security posture and advanced adversarial
settings. Use **System -> Policies** for auth/API-key/HTTPS/rate-limit YAML
changes; use **System -> Security** for live security dashboards and specialized
privacy/anonymity controls. Its local dashboard snapshot refreshes every 30
seconds only while visible, does not overlap slow requests, retains the last
successful result across transient failures, and mounts only the selected
Status or Adversarial pane.

## Network And Mesh

Use:

- **System -> Network** for Soulseek/DHT/mesh health and public exposure notes.
- **System -> Mesh** for mesh evidence, realm subject-index review, and
  conflict surfaces. It also includes Soulseek mesh rendezvous controls for the
  opt-in `slskdn-mesh-v1` public interest tag and runtime peer capability
  records discovered from signed slskdN descriptors. Transport status reads
  are passive: they report the last NAT result collected by mesh startup,
  descriptor refresh, or the explicit NAT-detection action without launching
  STUN probes from dashboard polling.
- **System -> Swarm Analytics** for multi-source/rescue performance review.

## Library Health

**System -> Library Health** surfaces scan status, issue review, reports,
replacement search seeds, quarantine review packets, and safe-fix manifests.
Exports are review-first and do not create remediation jobs or mutate files by
themselves.
