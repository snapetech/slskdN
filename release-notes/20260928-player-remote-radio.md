---
category: fixed
audience: users, operators
area: player
action: none
breaking: false
---
Listed radio obtains fresh local tickets pinned to the permitted host snapshot. Remote reads use the overlay transport identity and check host permission on every request. Single byte ranges support seeking; retries renew tickets. Reads are paced within the existing global RPC budget. Older hosts show an update-required state.
