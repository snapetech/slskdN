# Wishlist & Search Guide

## Overview

The Wishlist feature lets you save searches that run automatically on a schedule. Each wishlist item tracks its search history, visible hits, hidden locked hits, filtered hits, and downloads. This guide covers how to use wishlist features effectively.

## Wishlist Items

Each wishlist item has the following properties:

- **Search Text**: The search query (same format as the Search page)
- **Filter**: Optional filename/path or extension filter (for example `flac`, `flac OR mp3`, or `flac -"Inner Space Vol. 2"`)
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

- **Enable** all selected items
- **Disable** all selected items
- **Delete** all selected items

## Filter Presets

The wishlist modal provides quick-select filter buttons:

| Preset | Filter String | Description |
|--------|--------------|-------------|
| FLAC | `flac` | FLAC only |
| MP3 | `mp3` | MP3 only |
| FLAC + MP3 | `flac OR mp3` | Either format |
| FLAC + ALAC | `flac OR alac` | Apple Lossless or FLAC |
| Lossless | `flac OR alac OR wav OR ape` | All common lossless formats |
| Any | *(empty)* | Accept any file format |

Positive terms are alternatives. Prefix a word with `-` to reject paths containing it, or quote a phrase to keep its words together. For example, `flac -"Inner Space Vol. 2"` accepts FLAC paths except that specific release title.

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
