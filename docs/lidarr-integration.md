# Lidarr Integration

Lidarr support is built into slskdN as a first-class music acquisition workflow.
No Lidarr plugin is required. slskdN talks to Lidarr's supported HTTP API, pulls
Wanted/Missing albums and tracks into Wishlist, downloads through the normal
Soulseek queue, and can submit completed files back to Lidarr for safe import.

## What It Does

- Read Lidarr's wanted/missing album list from `/api/v1/wanted/missing`.
- Use each album's Lidarr quality profile to derive an entry-specific audio
  filter when the profile exposes recognizable formats such as MP3 or FLAC,
  including a `minbr:<kbps>` threshold for names such as `MP3-320KBPS`.
- Keep a genuinely empty album as one album-level Wishlist search. For a partial
  album, read `/api/v1/track?albumId=...` and create one Wishlist search per
  missing track instead of searching the complete album again.
- Optionally start those Wishlist searches in the normal slskdN download flow.
- Save completed files into a folder that Lidarr can also read.
- Use Lidarr's existing manual import and command APIs for safe post-download
  import automation.
- Expose operator endpoints under `/api/v0/integrations/lidarr/*` for status,
  wanted sync, wanted preview, and manual import.

This does not make slskdN appear as a native Lidarr download client in the
Lidarr UI. That would require either a Lidarr plugin or a compatibility layer
that impersonates a download client protocol Lidarr already supports. The
plugin-free design keeps the integration portable and avoids depending on
Lidarr internals.

## Setup Checklist

1. In Lidarr, copy the API key from Settings, General, Security.
2. Configure `integrations.lidarr.url` and `integrations.lidarr.api_key` in
   slskdN.
3. Make the slskdN completed-download directory readable by Lidarr. In Docker,
   this normally means mounting the same host path into both containers.
4. Enable `sync_wanted_to_wishlist` if Lidarr wanted albums should become
   slskdN Wishlist searches automatically.
5. Leave `auto_download` off until wanted sync produces the searches you expect.
6. Enable `auto_import_completed` only after Lidarr can see the same completed
   directory path, or after `import_path_from` / `import_path_to` is configured.

## Configuration

```yaml
integrations:
  lidarr:
    enabled: true
    url: "http://127.0.0.1:8686"
    api_key: "<lidarr-api-key>"
    sync_wanted_to_wishlist: true
    sync_interval_seconds: 3600
    max_items_per_sync: 100
    auto_download: false
    wishlist_filter: ""
    wishlist_max_results: 100
    auto_import_completed: true
    import_mode: "move"
    import_replace_existing_files: false
    import_path_from: ""
    import_path_to: ""
```

The Lidarr API key is available in Lidarr under Settings, General, Security.
For Docker installs, use a shared volume layout so both apps see completed
downloads at the same path or configure Lidarr remote path mappings.

The conservative default is `auto_download: false` and
`auto_import_completed: false`. Turn them on separately. Wanted sync is the best
first test because it only creates Wishlist entries. After the synced searches
look right, enable `auto_download`; after Lidarr can see completed files, enable
`auto_import_completed`.

## Wishlist Reconciliation

Each Wishlist item created by the current Lidarr sync stores the Lidarr album
ID, and track-level items also store the Lidarr track ID. The IDs let later syncs
update the same item, disable stale targets, and stop searching for tracks that
Lidarr has acquired.

The `wishlist_filter` setting is the fallback for entries without a usable
quality profile mapping. When Lidarr's profile contains allowed recognizable
audio formats, those formats replace the fallback for that entry. Recognizable
bitrate names become metadata thresholds too: `MP3-320KBPS` produces
`mp3 minbr:320`, so 128-kbps hits cannot satisfy that Wishlist item even when
they share the `.mp3` extension. When a Wishlist item has more than one
eligible bitrate, automatic selection prefers the highest known bitrate source
before source-ranking tie-breakers.

For a partial album, slskdN creates one track-level Wishlist item per missing
track and limits each one to a single automatic enqueue. A peer
directory may still contain other files in its search response, but the
track-level limit prevents the Wishlist item from enqueueing the complete
directory. Fully missing albums retain the existing album-level behavior so
one good peer can supply the album efficiently.

On the first sync after upgrading, an unmarked album row is claimed only when
its search text and filter match the configured legacy fallback. Other manually
created Wishlist rows are left alone.

The sync remains bounded by `max_items_per_sync`. It does not scan peers or
probe files beyond the Lidarr wanted list and the per-album track metadata
needed to identify missing tracks.

## Docker Volume Pattern

The simplest setup is to mount the same host directory into both containers at
the same path:

```yaml
services:
  slskdn:
    volumes:
      - /srv/media/downloads:/downloads

  lidarr:
    volumes:
      - /srv/media/downloads:/downloads
      - /srv/media/music:/music
```

With that layout, slskdN can complete downloads under `/downloads/music` and
Lidarr can import the same path without rewriting.

If slskdN and Lidarr see the completed directory under different paths, configure
the prefix rewrite in slskdN:

```yaml
integrations:
  lidarr:
    import_path_from: "/downloads/music"
    import_path_to: "/data/soulseek/music"
```

For example, a completed slskdN directory
`/downloads/music/Artist/Album` is sent to Lidarr as
`/data/soulseek/music/Artist/Album`.

For a Docker slskdN instance importing into native Windows Lidarr, the source
must be the path inside the slskdN container and the destination must be the
Windows path visible to Lidarr:

```yaml
directories:
  downloads: "/music/downloaded"

integrations:
  lidarr:
    auto_import_completed: true
    import_path_from: "/music/downloaded"
    import_path_to: 'D:\downloaded'
```

`import_path_from` does not change where slskdN stores files. It only rewrites
the folder sent to Lidarr's manual-import API. If both applications run on
Windows and see the same folder, leave both mapping values empty and configure
`directories.downloads` with the actual Windows path. Restart slskdN after
changing `directories.downloads` or the integration settings.

Use single quotes around Windows paths in YAML. A value such as
`"D:\downloaded"` is invalid YAML because the backslash begins an escape
sequence; `D:/downloaded` parses but Lidarr rejects it because its manual-import
API requires a full Windows path with backslashes.

## API

Use slskdN's API to verify and run the integration manually:

```bash
curl -H "X-API-Key: <slskdn-api-key>" \
  http://127.0.0.1:5030/api/v0/integrations/lidarr/status

curl -H "X-API-Key: <slskdn-api-key>" \
  http://127.0.0.1:5030/api/v0/integrations/lidarr/wanted/missing

curl -X POST -H "X-API-Key: <slskdn-api-key>" \
  http://127.0.0.1:5030/api/v0/integrations/lidarr/wanted/sync

curl -X POST -H "X-API-Key: <slskdn-api-key>" \
  -H "Content-Type: application/json" \
  -d '{"directory":"/downloads/music/Artist/Album"}' \
  http://127.0.0.1:5030/api/v0/integrations/lidarr/manualimport
```

## Import Behavior

When `auto_import_completed` is enabled, slskdN listens for completed download
directories and asks Lidarr for manual-import candidates for that directory.
The Web UI **Run Manual Import** button and the manual-import API endpoint are
separate operator actions: they bypass the automatic-import setting and the
normal recent-directory debounce, but still require the Lidarr integration to
be enabled and only submit clean, unambiguous candidates.
slskdN only submits candidates that Lidarr has already matched cleanly:

- no rejection reasons
- matched artist
- matched album
- matched album release
- at least one matched track
- parsed quality
- not an additional/non-track file

Rejected or ambiguous candidates are retained by default so the user can import
them interactively in Lidarr. Two independent, opt-in policies are available:

- `delete_rejected_downloads: true` deletes only the exact completed files that
  Lidarr rejected. It never recursively deletes the completed directory, which
  may be a shared root under flat download layouts.
- `blacklist_rejected_downloads: true` adds the exact Soulseek peer and remote
  release directory to the originating Wishlist item's ignored results. Future
  automatic searches skip that result without banning the peer. The rule is
  persistent and can be reviewed or removed through the Wishlist ignored-results
  controls/API.

These policies apply to automatic completed-directory events. Manual import API
calls do not delete files or add suppression rules because they lack the
originating transfer identity.

This is intentionally stricter than blindly accepting every manual-import
decision. A file is not auto-imported if Lidarr reports rejection reasons, cannot
match the artist/album/release/tracks, cannot parse quality, or marks the file
as an additional/non-track file.

## Recommended Rollout

1. Enable Lidarr with `sync_wanted_to_wishlist: false` and call `/status`.
2. Call `/wanted/missing` and confirm Lidarr returns the expected missing albums.
3. Run `/wanted/sync` with a low `max_items_per_sync`, such as `10`.
4. Review the created Wishlist searches.
5. Enable `sync_wanted_to_wishlist` for scheduled sync.
6. Enable `auto_download` if you want Lidarr-seeded Wishlist items to start
   downloading automatically.
7. Enable `auto_import_completed` after the completed-download path is visible
   to Lidarr and path mapping has been tested.

## Manual Operation

Run a one-time wanted sync:

```bash
curl -X POST -H "X-API-Key: <slskdn-api-key>" \
  http://127.0.0.1:5030/api/v0/integrations/lidarr/wanted/sync
```

Ask slskdN to import a completed directory through Lidarr. This manual action
is available while `auto_import_completed` is false, can be retried immediately,
and returns `candidateCount`, `safeCandidateCount`, `commandId`, and
`skippedReason` fields describing what happened:

```bash
curl -X POST -H "X-API-Key: <slskdn-api-key>" \
  -H "Content-Type: application/json" \
  -d '{"directory":"/downloads/music/Artist/Album"}' \
  http://127.0.0.1:5030/api/v0/integrations/lidarr/manualimport
```

The manual fallback flow is:

1. Configure slskdN's completed download directory where Lidarr can read it.
2. Run the wanted sync or enable `sync_wanted_to_wishlist`.
3. Let Wishlist search and download the album or its missing tracks.
4. In Lidarr, use Wanted, Manual Import on the completed-download folder.
