# Listening Party

Last updated: 2026-09-27

slskdN listening parties are metadata-synchronized playback sessions for pods. They intentionally do not relay music bytes through the host.

## Layer 1: Listen Along

The first implementation ships a persistent web player and a pod listen-along protocol:

- The host plays a local/shared item by `ContentId` through `GET /api/v0/streams/{contentId}`.
- The player updates `NowPlayingService` directly, so the Soulseek profile reflects playback started from the web UI.
- The host can publish pod-scoped playback metadata: `play`, `pause`, `seek`, and `stop`.
- Listeners receive metadata over SignalR and load their own stream for the same `ContentId`.
- Playback messages are stored as pod messages and routed through the existing pod message router.

The protocol payload is JSON in the pod message body:

```json
{
  "kind": "slskdn.listenAlong.v1",
  "podId": "pod:example",
  "channelId": "general",
  "hostPeerId": "alice",
  "action": "play",
  "contentId": "content:audio:file:...",
  "title": "Track title",
  "artist": "Artist",
  "album": "Album",
  "positionSeconds": 15.2,
  "serverTimeUnixMs": 1777500746000,
  "sequence": 42
}
```

`serverTimeUnixMs` and `positionSeconds` let listeners compensate for elapsed time when joining a currently playing party. Clients should treat host state as advisory and keep user control local: following a party can be toggled off without leaving the pod.

Following an already paused room selects the host track at its paused position. A host Stop event ends following and clears the local player.
If a host Pause arrives while the listener is still obtaining a stream ticket, the player cancels the pending start so the track remains paused when its source becomes ready.
After a connection interruption, a follower rejoins the room and refreshes the host's current state before continuing.
Pressing Stop or choosing another track in the local player also leaves follow mode; later host updates will not restart or replace the listener's playback.
Hiding the browser player leaves Follow even if a room is idle. Show the player again before following a room.

Broadcasts use the host player's current absolute position, including a decoded stream's start offset. A listed radio announcement also carries its latest action and position so a directory listener can join at the advertised point and stay paused if the host is paused. Browser-only files cannot be broadcast because other listeners cannot access their object URLs.
Listed radio starts playback from the latest directory snapshot. It does not subscribe to later changes from a different pod; use the room's Follow control for live updates in a pod you have joined.

## Web Player

The Web UI player is part of slskdN itself. It is not a separate streaming server, and it does not require an external media service.

### Playback Sources

The player can start playback from:

- Collection rows and collection-item play buttons.
- The player empty-state **Collections** browser, which opens a two-pane modal with collection list and playable items.
- The player empty-state **Files** browser, which opens a searchable modal over shared and downloaded local audio.
- Pod/listening-party follow actions that resolve the announced `ContentId`.
- Audio files chosen from the current device. These play through browser object URLs for this session and are not uploaded or saved to server Collections.

File-browser breadcrumbs and folder rows explain where they navigate on hover. Queue preview chips explain that clicking removes an upcoming track without stopping the current one; listen-along icon actions have spoken labels for assistive technology.
The Collections picker clears the old track list while a new Collection loads and ignores delayed results from previously selected Collections.
The player requests the Collections list when that browser opens, leaving ordinary transport use free of the extra library request.
The local file search stops showing Loading when a request is canceled by closing the picker or shortening the query below two characters.
Explicit file searches can find configured local downloads before share indexing; this fallback reads file metadata without hashing audio. Opening the Files browser without a query does not recursively scan local directories. The returned page is registered for playback so switching to another found download does not wait for the fallback scan cooldown.
Collapse is available with player tools closed. On narrow screens the compact bar keeps the title and primary transport visible; expand it to reach the additional controls.

All normal playback uses `GET /api/v0/streams/{contentId}`. That endpoint supports byte ranges, seeking, content-type detection, authenticated access, share-token access where applicable, and per-user stream limiting.

For local browser picking, slskdN can resolve streamable IDs from configured non-excluded share directories and the configured downloads directory. This keeps downloaded/shared audio playable even before a row has been persisted into `content_items`, while still keeping file access scoped to configured local roots.

### Player Controls

The persistent player is docked above the fixed footer and can collapse into a small drawer bar. It should never cover the footer.

Controls include:

- A compact bar with play/pause, previous/next, elapsed and remaining time, seek, volume, and queue access.
- An expanded view with stop, rewind/fast-forward, playback speed, shuffle, repeat off/all/one, and additional tools behind **Show player tools**.
- Queue reorder and Play Next from the player's library browsers. The immediate queue, selected track and latest playback position survive a browser refresh in session storage but do not autoplay; saved playlists use Collections. Position is checkpointed when the tab hides or leaves. Previous and Next start the selected queue occurrence from the beginning.
- Browser-local files and listen-along or listed-radio streams with short-lived URLs stay in the live queue but are not restored after a refresh. Remaining server-library tracks can request fresh stream tickets.
- Saving the queue to a Collection playlist also omits temporary radio streams and browser-local files, because those URLs cannot be replayed from a saved content ID. Save and Load show progress and temporarily disable playlist fields until the operation finishes. A playlist saved while the list is still loading remains available.
- Clear Upcoming removes future tracks while preserving played history for Previous and repeat-all.
- Browser-local mute. This mutes only the current browser or installed PWA; it does not stop the stream or mute other listeners.
- Browser Media Session metadata and transport handlers for supported mobile/PWA lock-screen controls.
- Keyboard shortcuts apply when focus is outside buttons, links, sliders, and text controls, leaving normal keyboard operation of those controls intact.
- Optional MilkDrop visualizer, lightweight analyzer, equalizer, synced lyrics, crossfade, karaoke-style center-channel reduction, and ListenBrainz now-playing/scrobble submission when the relevant player controls are enabled.

The optional Picture-in-Picture spectrum follows the active audio element across crossfades and player layout changes. It draws less often while playback is paused and closes when the player is hidden. Hiding the player also cancels a pending window open; an opening failure appears in the player without stopping playback.
The full visualizer module loads only when its tile is activated; ordinary playback and the lightweight analyzer do not need to load it.
Turning the visualizer off returns its tile to album art while preserving the selected engine for later use.
The lightweight spectrum and scope canvases cap drawing near 30 frames per second and reuse audio buffers while active; they pause drawing when the document is hidden. The small analyzer starts Off and cycles through Spectrum, Scope, and Off when clicked. Its chosen mode is saved in this browser. When upgrading from the older always-on analyzer, the automatically saved Spectrum default becomes Off; a previously selected Scope mode is preserved.

The player publishes Now Playing after playback begins and clears it on pause, stop, failure, or final track end. Playback errors are shown in the player. If the browser cannot decode a server library audio file, **Decode for playback** requests a short-lived, ticket-bound MP3 stream from the configured FFmpeg executable. It runs only on demand, permits one decode per user and two per server, and never sends browser-chosen files to the server. Server administrators can edit tags on indexed local audio from a Collection item; collection display metadata can be edited separately without changing the file. Tag edits change file bytes and may change its content ID, so the server refreshes the share index and Collection references.
Browser Now Playing changes are sent in playback order, so a slow Play update cannot overwrite a later Pause or Stop clear.
While a new track's stream ticket is loading, events from the previous media source do not advance the queue or count toward the new track's listening history. Play and Pause during ticket or decode setup apply to the selected track when its source is ready. Re-selecting a track with the same URL restarts it; if an incoming crossfade cannot start, retry targets the selected track.

Seeking, rewind/fast-forward, Previous, and repeat use the full track position during decoded playback. Seeking while paused leaves playback paused. Repeated rewind/forward actions during decoded stream setup accumulate from the requested position, and collapsing or expanding the player preserves pending playback intent. The seek slider announces elapsed and total time to assistive technology.
The audio output selector switches the Web Audio contexts that carry playback, waits for both audible crossfade tracks, rolls back on failure, and routes a new context before it starts playback. It appears only when the browser supports AudioContext output selection. Where supported, use the adjacent headset button to grant access to another speaker or headset; the list refreshes when devices change.

Listening history and optional ListenBrainz scrobbles count actual playback progress toward the track threshold. Skipping forward does not count the skipped portion as listening. Pause and Stop also silence both audio elements if a crossfade is in progress.
If the incoming track errors during a crossfade, the outgoing element stops immediately so the visible failure and audible output agree.
Turning crossfade off or seeking during a fade also stops the outgoing track immediately. A later pass through the end of the track can start a new fade.
Crossfade uses Web Audio for independent track gains. When a browser does not provide Web Audio, track changes remain direct and the selected volume stays intact.
Plain playback through the system default speaker starts without a Web Audio graph when the equalizer is flat, karaoke, crossfade, and visualizers are off. Enabling an audio effect or choosing a custom speaker during playback creates and resumes the graph then; the active media element keeps that graph until it is replaced.
Headset and lock-screen Play use the same loading and error behavior as the visible player Play control.
Crossfade respects Repeat One. With Repeat All, it can also fade from the final queued track back into played history.
Its gain ramps follow the outgoing track's remaining time and playback speed, including when the incoming track takes time to start.
Listen-along Broadcast and Stop actions are sent in click order. A rapid Stop uses the Broadcast party ID to remove its listed radio entry. Switching rooms clears the prior room's details and keeps party IDs in their own rooms. A failed latest action shows an error next to the room controls so it can be retried.
Seeking while paused keeps the player in Paused even if the browser fetches more audio data.
A late Pause event after a media failure leaves the player in Error until the user retries or selects another track.
Volume, local mute, and playback speed changes affect both audio elements while a crossfade is in progress.
Equalizer and karaoke changes also affect both tracks during a crossfade.
Saved equalizer bands are restored within the slider range. Lyrics clear when a new track lookup starts, and a canceled lookup cannot replace the newer result.

Synced lyric highlighting follows the browser media element's playback, seek,
and metadata events. It does not run a separate fixed polling timer, and hidden
documents defer position updates until visibility returns.

The browser owns audio output. A listener can keep following a party while locally muted, and the host can keep playing locally while publishing metadata.
Invalid saved volume or playback speed values reset to usable defaults when the player loads.

### External Visualizers

The built-in player visualizer uses Butterchurn in the browser. slskdN can also expose an opt-in external visualizer launcher for host-side tools such as MilkDrop3. Configure `player.external_visualizer` in `slskd.yml`, then use **Player Integrations** to check status or launch it.

External launches run on the slskdN host process, not on the browser device. This works best when slskdN runs in the same desktop session as the visualizer and the visualizer can capture system audio or a virtual audio device. Docker, service, SSH, and headless deployments usually need a wrapper script or will not have a display/audio session.

The browser cannot provide executable paths or arguments. The launch endpoint only uses configured values from `slskd.yml`, is disabled by default, requires normal API auth, and does not contact Soulseek peers.

The long-term target is not the external launcher. The native plan is a WebGL2-first MilkDrop3-compatible engine inside slskdN, with Butterchurn kept as the current browser renderer and the external launcher kept only as a bridge for users who want desktop MilkDrop3 today. See [WebGL MilkDrop3 Port Plan](design/webgl-milkdrop3-port.md).

## Network And Rights Boundary

Layer 1 is deliberately conservative. It broadcasts only metadata and relies on the existing stream endpoint and authorization boundary for bytes:

- Normal authenticated users can stream content their node can locate.
- Share tokens still control shared collection streaming.
- Pod messages do not grant a new right to bytes.
- No peer is asked to browse, probe, download, or relay media automatically.

This keeps listening parties aligned with slskdN network-health rules: user-triggered playback, no aggressive scanning, and no surprise bandwidth fan-out from the host.

## Layer 1.5: Global Radio Registry

Hosts can explicitly opt in to listing a party in the slskdN radio directory. This is still integrated into slskdN:

- Directory API: `GET /api/v0/listening-party`
- Host publish API: `POST /api/v0/listening-party/{podId}/{channelId}`
- Integrated radio stream: `GET /api/v0/listening-party/radio/{partyId}/{contentId}`

The host controls two separate toggles:

- **List globally** publishes a `slskdn.listeningParty.announce.v1` announcement into the mesh-DHT-backed party index; the public BitTorrent DHT is only endpoint rendezvous.
- **Mesh streaming** allows listeners who find the listing to stream the current track directly from the host's slskdN node.

The registry announcement is TTL-based and contains metadata plus a relative stream path when mesh streaming is enabled:

```json
{
  "kind": "slskdn.listeningParty.announce.v1",
  "partyId": "party:...",
  "podId": "pod:...",
  "channelId": "general",
  "hostPeerId": "alice",
  "title": "Track title",
  "artist": "Artist",
  "contentId": "content:audio:file:...",
  "allowMeshStreaming": true,
  "streamPath": "/api/v0/listening-party/radio/party%3A.../content%3Aaudio%3Afile%3A...",
  "expiresAtUnixMs": 1777501646000
}
```

Directory hydration is deliberately conservative. The current compact pod
panel does not request the global directory because it does not display it.
Full directory views refresh only while visible, at most once per minute, and
do not overlap slow requests or discard the last successful listing after a
transient failure. On the server, concurrent HTTP callers share one DHT index
and announcement refresh, and the resulting snapshot is reused process-wide
for one minute.

The integrated radio endpoint only serves the active party's current `ContentId`, only while the host has both listing and mesh streaming enabled, and only while the normal streaming feature is enabled. It uses the same content locator and stream session limiter pattern as `/api/v0/streams/{contentId}`.

## Deferred: Live Mic / Host Commentary

Live microphone or host audio broadcast is a later layer. It should use opt-in WebRTC media with SDP/ICE signaling carried by pod messages, and it needs a separate rights and moderation review before public/listed pods can expose it.

### Server formats and decoded seeking

The Files picker includes AIFF (`.aif`/`.aiff`), ALAC, APE, M4B and WMA alongside
the usual MP3, FLAC, Ogg, AAC, M4A and WAV audio. If the browser cannot play a
server file directly, use **Decode for playback** to request an MP3 stream from
FFmpeg. Decoding uses server CPU only for the requested playback.

Decoded seeks use the original track timeline. A burst of keyboard or slider
inputs updates the displayed position immediately and combines setup work
until input settles. The old media request is aborted before replacement,
and seeking while paused keeps playback paused.

Play Next and bulk queue additions use the same title, artist and artwork defaults as direct Play, including filename-only library items and restored sessions.

Browser media controls support Play, Pause, Stop, Previous, Next and seeking where the browser exposes those actions. Stop clears the current media metadata and position as well as the playback source.

Stop also ends analyzer sampling and closes the floating Picture-in-Picture analyzer. Pause freezes the inline analyzer until playback resumes.

Existing Web Audio contexts suspend on Pause or Stop and resume before playback. Outgoing crossfade contexts suspend after their media stops. These cleanup paths do not create new audio graphs.

## Listed radio and connection recovery

Open player tools and choose **Open listed radio**. No current track is required.
Opening the picker loads the directory; **Refresh** updates it manually. Each
entry shows whether its host enables streaming. Metadata-only entries cannot be
played. Play uses the current track snapshot and its announced position; rejoin
for later track changes. Temporary URLs are excluded from saved playlists and
browser refresh recovery.

Remote snapshots use the host-scoped mesh route described below. An empty mesh
DHT routing table learns responder identities on demand through at most three
existing outbound overlay neighbors. Directory lookup creates no new peer
connections. Playback still requires the announced host to be connected.

Joining a live listen-along subscription requires pod membership, just like reading
the room playback state. Banned members and accounts outside the pod cannot join;
administrators retain their existing access. This check applies when joining the
subscription.

A listen-along panel shows connecting/offline state and reports failed room-state
refreshes. Use **Retry listen-along connection** after startup failure, a closed
connection, failed rejoin or refresh failure. Retry recreates the connection,
rejoins the room and refreshes the host snapshot. Existing automatic reconnect
continues to handle established connections. Radio stream failure retries its
own source; it does not request local-library decode metadata.


## Listed-radio host routing

Listed radio is a manually selected track snapshot. Current hosts publish their
overlay Soulseek transport username separately from the web account displayed
in the directory. The listener requests a fresh local ticket rather than
opening the host's relative URL. Every remote read verifies that the host still
lists that party, permits streaming, serves the selected content and accepts the
party capability. A track change, revocation or expired capability interrupts
the snapshot; refresh the directory and select a current entry.

Both nodes need streaming enabled; remote playback also needs mesh enabled and
an existing outbound mesh service connection to the host. Playback does not
discover extra peers or request Soulseek downloads. Locally hosted snapshots
retain their direct HTTP stream with a fresh ticket. Older directory entries
without transport metadata show **Host update required**.

Remote reads support one HTTP byte range at a time, use at most 44 KiB per
request and are paced at no more than five requests per second. The listener
allows one active radio stream to each host, sharing the existing global
500-call-per-minute peer budget. High bitrate audio or slow connections may
buffer. Press Play to retry initial ticket or media failures; retry obtains a
fresh local ticket. Temporary radio streams are not restored after reload.

Real loopback TLS checks establish remote byte delivery and host permission
revocation. They do not establish sustained radio quality across real network
latencies, physical devices or simultaneous listen-along participants; see
the [player quality audit](dev/player-quality-audit.md).

Radio seeks retain the fairness admission of their short-lived ticket. A new
radio ticket still checks the existing fairness policy. Replacing a byte range
for the same ticket ends its preceding HTTP response; other tickets cannot
preempt it. One active stream per owner and host remains enforced.

## Stalled radio and manual refresh

Use **Retry radio playback** if a radio snapshot buffers indefinitely or reports
an error. This action is available in both player layouts. It releases the old
source and reconnects at the current position, provided the host still permits
streaming. A revoked host cannot be bypassed by retrying.

The directory's **Refresh** action requests current announcements rather than
reusing the normal one-minute cache. Concurrent refreshes share one lookup;
repeated manual refreshes within two seconds reuse recent results. Withdrawn
remote listings disappear after a successful index refresh. Background
directory polling keeps its existing cache behavior.

Radio traffic totals count successful audio payload bytes after overlay reply
writes, excluding metadata and framing overhead. The host records uploads and
the listener records received data. Expired capabilities and revoked reads earn
no upload credit. New playback tickets still check the existing fairness policy.
