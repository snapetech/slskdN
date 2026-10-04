---
category: fixed
audience: users, operators
area: collections-sharing
action: none
breaking: false
---
Collection database upgrades now check for missing columns explicitly and stop startup on real schema errors instead of hiding them as already-applied migrations.
