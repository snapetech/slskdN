# Bug Council Sibling-Search Rule

When the council accepts and fixes a finding, the same shape almost always exists at other call sites. The single most common council failure mode is closing a row on the literal hit and missing its twins. The sibling-search rule prevents that.

## Rule

Before any sweep row is moved to **Fixed**, the agent closing the row must:

1. Identify the **fix shape**: the validator name, guard call, or invariant that resolves the original finding (e.g. `ProtocolCountReader.ReadValidatedCount`, `MessageFrameValidator.ValidateMessageLength`, an immutable-snapshot copy in a constructor).
2. Run a sibling search across the codebase for the **call shape that the original bug had**, not the fix shape. If the bug was `new byte[reader.ReadInteger()]`, the sibling search is `rg "new byte\[[^]]*Read(Integer|Long)"`.
3. For every hit, record one of:
   - **Same shape, fixed** — record the file:line and confirm the fix is in place.
   - **Same shape, also bug** — open a new sweep row at the same severity.
   - **Different shape, N/A** — write a one-line reason the hit doesn't apply (different boundary, already trusted input, etc.).
4. Only after all sibling hits are accounted for can the row be marked Fixed.

## Output format

The closing rationale on the original row gets a `Sibling search:` line:

```
Sibling search: rg "new byte\[[^]]*ReadInteger" src/Messaging
  src/Messaging/Foo.cs:42 — fixed (this row)
  src/Messaging/Bar.cs:88 — fixed (RT-074, prior sweep)
  src/Messaging/Baz.cs:113 — N/A, length already validated by frame guard
  src/Messaging/Qux.cs:201 — opened RT-117
```

This makes the sweep self-documenting: a future reader can see what was searched, what was found, and where each hit went.

## When the search is too noisy to enumerate

If the sibling shape produces hundreds of hits, narrow the search by adding the originating file's directory or by combining patterns. Do **not** waive the rule on noise alone — a noisy shape is exactly the kind that hides siblings. If after narrowing the result is still too large for a single rationale block, split the sibling-search output into a new row in the registry's `Sibling Searches` section and reference it from the closing row.

## Interaction with the negative-space gate

The negative-space gate (`docs/dev/bug-council-negative-space.md`) declares known trust boundaries and the validator each must run. The sibling search complements it: the gate says "this boundary must call this validator," the sibling search says "any code shaped like the bug we just fixed must also be examined." Use both. Neither subsumes the other.

## Why this rule exists

In the 2026-05-05 sweep cycle, three rows in the protocol-scalar register fixed individual untrusted-scalar emission sites without sweeping siblings; the next sweep had to reopen the same class to catch those misses. The cost of that re-sweep was higher than the cost of the per-row sibling check would have been. The rule converts a recurring expensive failure mode into a small fixed per-row cost.
