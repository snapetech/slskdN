# Wishlist & Search Guide

## Overview

The Wishlist feature lets you save searches that run automatically on a schedule. Each wishlist item tracks its search history, visible hits, hidden locked hits, filtered hits, and downloads. This guide covers how to use wishlist features effectively.

## Wishlist Items

Each wishlist item has the following properties:

- **Search Text**: The search query (same format as the Search page)
- **Filter**: Optional filename/path, extension, or metadata filter (for example `flac`, `flac OR mp3`, `mp3 minbr:320`, `mp3 minbr:320 OR aac minbr:256 OR m4a minbr:256`, or `flac -"Inner Space Vol. 2"`)
- **Enabled**: When enabled, the item is searched automatically on each scheduler cycle
- **Auto-download**: When enabled, best-matching files are downloaded automatically
- **Max Results**: Maximum number of responses to accept per search
- **Max Downloads**: Auto-disable after N successful downloads (leave blank for one-shot behavior)

## Auto-Disable Behavior

When auto-download is enabled:

- **Default (Max Downloads blank)**: The item is disabled after the first successful download. This is "one-shot" behavior — useful for finding a specific album.
- **Max Downloads set (e.g., 5)**: The item stays enabled until the total download count reaches that number. Useful for multi-part releases where each search finds one piece.

## Unseen Results Badge

Wishlist items show a red **"N new"** badge when there are visible hits from the most recent search that you haven't viewed yet. Locked results hidden by normal search filtering are counted separately in the hit tooltip, but they do not inflate the visible new-results badge. The badge clears when you:

- Expand the search history for that item
- Click "Mark All Viewed" in the header
- View the linked search results page

## Search History

Each wishlist item tracks its search history. Click the **angle-down** button on an item to see:

- All past searches for that item
- Source badge (wishlist / auto-replace / manual)
- Response count and file count per search
- **Inline results**: Click the angle-down on any search to see results without leaving the page

## View Modes

The Wishlist page supports two view modes:

- **Table view**: Traditional columnar layout with checkboxes for bulk selection
- **Card view**: Expandable cards showing search text, filter badge, stats, and quick actions inline

Toggle between them using the table/grid buttons in the header.

The toolbar can filter the page to matching text, new results only, enabled items, auto-download items, or items with visible hits. Sort options include newest added, alphabetical order, last searched, new results first, and most visible hits. These view choices are saved in the browser.

## Bulk Operations

Select multiple items using checkboxes, then use the action bar to:

- **Edit filters** for all selected items, including applying `mp3 minbr:320`
  or clearing filters by leaving the field blank
- **Enable** all selected items
- **Disable** all selected items
- **Delete** all selected items

The bulk filter action updates existing Wishlist rows in one atomic request; it
does not require deleting and re-adding them, and a missing ID causes the whole
operation to roll back. A later Lidarr wanted sync can refresh a Lidarr-owned
row back to the quality profile's filter.

## Filter Presets

The wishlist modal provides quick-select filter buttons:

| Preset | Filter String | Description |
|--------|--------------|-------------|
| FLAC | `flac` | FLAC only |
| MP3 | `mp3` | MP3 only |
| MP3 320+ | `mp3 minbr:320` | MP3 at 320 kbps or higher |
| FLAC + MP3 | `flac OR mp3` | Either format |
| FLAC + ALAC | `flac OR alac` | Apple Lossless or FLAC |
| Lossless | `flac OR alac OR wav OR ape` | All common lossless formats |
| Any | *(empty)* | Accept any file format |

Positive terms within one branch are filename alternatives. `OR` starts a new
branch, so each branch keeps its own metadata constraints. Prefix a word with
`-` to reject paths containing it globally, or quote a phrase to keep its words
together. For example:

```text
mp3 minbr:320 OR aac minbr:256 OR m4a minbr:256
```

accepts MP3 files at 320 kbps or higher, or AAC/M4A files at 256 kbps or
higher. It does not accept MP3 at 256 kbps, AAC at 192 kbps, or a FLAC file.
This form is different from `mp3 OR aac minbr:256`: the first branch has no
bitrate floor, while the second branch does. A global exclusion applies to
every branch, for example `flac minbr:800 OR mp3 minbr:320 -demo`.

`minbr:<kbps>` (also accepted as `minbitrate:<kbps>`) checks the bitrate
reported in search-result metadata. A value of `minbr:320` rejects a 128-kbps
MP3 even when its filename has the right extension. A missing bitrate never
satisfies a bitrate floor, including for mesh-derived results. Mesh discovery
does not apply Soulseek operator/server term suppression; the user's local
format, bitrate, path, and peer-ignore filters still apply.

For automatic downloads, slskdN first keeps only files that satisfy one
complete branch, groups them by peer and directory, and chooses the best copy
of each track name so an album does not enqueue duplicate MP3/FLAC copies. It
prefers coverage up to the item's remaining download limit, then compares
quality: lossless formats rank above lossy formats, and lossy codecs use a
codec-aware effective bitrate rather than comparing AAC/Opus/MP3 numbers as if
they were identical. Known metadata beats unknown metadata; source availability
and ranking break quality ties.

## Ignoring One Persistent False Positive

Wishlist search results provide **Ignore for Wishlist** beside each peer folder. It permanently hides only that peer's copy of that folder from the current wishlist item. The peer's other folders and results remain visible, and the same folder can still appear in unrelated searches.

Ignored folders are excluded from visible-hit counts, album candidates, and automatic-download selection. Edit the wishlist item to review its ignored folders and restore any folder. The ordinary red close action remains temporary and only hides the peer response until the current search view resets.

## Search Retention

Searches accumulate over time. To prevent the database from growing indefinitely:

- **Max Age (days)**: Searches older than this are automatically deleted
- **Max Count**: Only the N most recent searches are kept; oldest are deleted first
- **Cleanup Interval**: How often the automatic cleanup runs (default: 86400 seconds = daily)

You can also trigger manual cleanup from the Searches page or via API:

```
POST /api/v0/searches/cleanup?maxAgeDays=30&maxCount=1000
```

## Search Sources

Each search is tagged with its source:

| Source | Color | Description |
|--------|-------|-------------|
| Manual | Grey | Started from the Search page by the user |
| Wishlist | Blue | Triggered by a wishlist item's scheduler |
| Auto-Replace | Orange | Triggered by the auto-replace feature for file replacements |

Use the source filter on the Searches page to view only searches from a specific source.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v0/wishlist` | List all wishlist items |
| POST | `/api/v0/wishlist` | Create a new wishlist item |
| PUT | `/api/v0/wishlist/{id}` | Update a wishlist item |
| PUT | `/api/v0/wishlist/bulk-filter` | Atomically apply one filter to selected wishlist item IDs |
| DELETE | `/api/v0/wishlist/{id}` | Delete a wishlist item |
| POST | `/api/v0/wishlist/{id}/run` | Run a wishlist search now |
| GET | `/api/v0/wishlist/{id}/searches` | Get search history for an item |
| GET | `/api/v0/wishlist/{id}/ignored-results` | List persistently ignored peer folders |
| POST | `/api/v0/wishlist/{id}/ignored-results` | Ignore one peer folder for this item |
| DELETE | `/api/v0/wishlist/{id}/ignored-results/{ignoredResultId}` | Restore an ignored peer folder |
| POST | `/api/v0/wishlist/{id}/mark-viewed` | Mark one item as viewed |
| POST | `/api/v0/wishlist/mark-all-viewed` | Mark all items as viewed |
| POST | `/api/v0/wishlist/import/csv` | Import wishlist items from CSV |
| POST | `/api/v0/searches/cleanup` | Run search retention cleanup |
| DELETE | `/api/v0/searches` | Delete all completed searches |

Bulk filter updates send the selected IDs and one filter expression together:

```json
{
  "ids": ["<wishlist-id-1>", "<wishlist-id-2>"],
  "filter": "mp3 minbr:320 OR aac minbr:256 OR m4a minbr:256"
}
```
