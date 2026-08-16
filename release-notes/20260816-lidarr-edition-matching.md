---
category: added
audience: users, operators
area: wishlist
action: none
breaking: false
---
Wishlist auto-download now checks a Lidarr-synced item's expected track count, duration, and release edition before downloading, and skips files that match something already completed elsewhere, cutting down on wrong-edition Lidarr import rejections and duplicate copies. Configure strictness with `integrations.lidarr.edition_match_mode`.
