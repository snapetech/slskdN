---
category: fixed
audience: users, operators
area: library-items
action: none
breaking: false
---
Library item lookup and hashing now stop cleanly when the request is canceled, instead of returning partial metadata from fallback paths.
