# Messaging Pane Redesign

Design contract, principles, layout, and phased plan for rebuilding the
`/messages` workspace as a compact, IRC/Discord-style chat surface with clear
delineation between Soulseek (DMs, rooms) and Mesh (pod channels).

Status: **proposal**, awaiting sign-off on the three open questions in §9.

---

## 1. Diagnosis

The current pane (`Messaging.jsx`, 1184 lines) suffers from:

- **Big-furniture syndrome.** Every conversation is a Semantic-UI `Card` with
  a thick title bar, five mini-buttons, padded body, and a member list block.
  One open panel fills the whole right side; two become a vague grid of chunks.
- **Sidebar of mini-toolbars.** Each row in *Saved Chats / Joined Rooms / Pod
  Channels* is a two-button row with its own padding. Signal-to-chrome ratio
  is poor: username + unread badge surrounded by ~60% chrome.
- **No transport identity.** Soulseek DM, Soulseek room, and Mesh pod channel
  all render as the same beige `Button basic`. Only an icon hints at the
  network and it is not legible at a glance.
- **No density control.** Semantic-UI's spacing is hardwired. "Old eyes" today
  means zooming the entire app, which doubles the sidebar.
- **Buttons everywhere actions belong on right-click.** Leave room, delete
  thread, open profile sit visible at all times. IRC and Discord put these
  behind context menus and hover.
- **Members rail competes with messages.** For a 4-person pod it eats ~25% of
  horizontal space.

## 2. Design contract (what does NOT change)

This is the boundary so the redesign is a UI swap, not a rewrite of the
daemon contract.

| Layer | Stays as-is |
|---|---|
| Data libs | `lib/chat.js` (`getAll`, `remove`, `sendBatch`), `lib/rooms.js` (`getJoined`, `getAvailable`, `join`, `leave`), `lib/pods.js` (`list`, `get`, `getMessages`, `getMembers`, `sendMessage`, `leave`) |
| Routing | `/messages`, `/messages/pods/:podId/:channelId?`, `initialKind` prop |
| Storage key | `slskd-messaging-workspace` — bumped to `v2` schema with a one-shot migrator |
| Polling cadence | 10s hydrate, 2s message refresh — unchanged for v1 of the redesign |
| Backend | Zero C# changes |

What gets touched, in JSX-land only:

- `components/Messaging/Messaging.jsx` rewritten as a shell.
- `ChatSession`, `RoomSession`, and the inline `PodChannelSession` collapse
  into one `<MessageStream>` plus a transport adapter. Their public props
  (`username`, `roomName`, `channel`) become a single `target` object.

## 3. Design ideals

The rules every PR has to obey.

1. **Density first.** Every row, gutter, and button defends its pixels.
   Default line-height 1.25; default avatar 16px and off unless toggled on.
2. **Transport identity is structural, not decorative.** Soulseek and Mesh get
   separate top-level sections in the channel tree, distinct accent color
   tokens (`--xport-slsk`, `--xport-mesh`), and distinct prefixes (`@user`,
   `#room`, `&pod/channel`). The eye should know which network a message lives
   on without reading the header.
3. **Keyboard is primary.** `Ctrl+K` quick-switcher, `Alt+↑/↓` cycle tabs,
   `/command` composer, `Esc` closes overlays. Mouse is fallback.
4. **Actions hide until needed.** Right-click for context menu; hover reveals
   leave/delete; chrome does not carry the affordance permanently.
5. **User-controlled density.** A persistent zoom control (S / M / L / XL)
   sets a single CSS custom property `--msg-scale`. Font, gutters, and avatar
   all derive from it. This is the "old eyes" lever without bloating the
   default.
6. **Progressive disclosure of metadata.** Member list and listen-along live
   in a collapsible right rail, off by default for DMs, on for rooms/pods,
   remembered per-target.
7. **Don't fight the protocol.** IRC vocabulary where it fits (`/me`, `/msg`,
   `/join`, `/part`, nick coloring). Discord layout where it fits (3-pane,
   network rail). No invented metaphors.
8. **One panel at a time, with tabs.** Drop the card-grid workspace metaphor.
   Browser-style tabs over a single message view. Power users get a
   `Split right` action that opens a second pane (max 2).

## 4. Layout

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [Top app nav unchanged: Search · Discovery · ... · Messages · ...]           │
├────┬───────────────────┬─────────────────────────────────────────────┬───────┤
│ S  │ SOULSEEK   ⚙      │ #producers              ≡ pin  ⤢ split  ⋯  │ MEMBERS│
│ ▓  │  @ alice    ●3    ├─────────────────────────────────────────────┤ (12)  │
│ ▓  │  @ bob              16:42  alice  hey, did you grab that flac?  │ alice ●│
│    │  # producers ●     16:42         the 24-bit one i mentioned     │ bob   │
│ M  │  # 80s-rock        16:43  bob    yeah pulling now               │ carol │
│ ▓  │                    16:43         3 of 4 sources agree on hash   │ ...   │
│    │ MESH               ──── new ─────────────────────────────────── │       │
│    │  & gold★/general●1 16:51  carol  /me waves                      │       │
│    │  & gold★/listenalong       carol started listenalong: Aphex Twin│       │
│    │  & dev-pod/general                                              │       │
│    │                    [Type a message, /help for commands]    [▶]  │       │
└────┴───────────────────┴─────────────────────────────────────────────┴───────┘
 ▲    ▲                  ▲                                               ▲
 │    │                  │                                               │
 │    │                  └─ Active conversation (tabbed above if >1)     │
 │    └─ Channel tree, IRC-style. Sections collapsible. Unread = ●N.     │
 │       Section accent colors reinforce transport.                      │
 └─ Network rail (40px). S = Soulseek, M = Mesh. Click filters tree.     │
                                                                         │
                                                Members rail, toggleable ┘
```

Two resize handles, both persisted in `localStorage`:

- channel-tree ↔ message-view splitter
- message-view ↔ member-rail splitter

A toolbar pill in the top-right of the message view: `[ S | M | L | XL ]`
density. That is the "old eyes" lever.

## 5. Component breakdown

```
<Messaging>                    shell, owns layout + zoom + active target
├── <NetworkRail/>             40px, S/M filters
├── <ChannelTree/>             grouped by transport
│   ├── <TreeSection title="Soulseek" accent="slsk">
│   │   ├── <DirectGroup conversations={...} />
│   │   └── <RoomGroup rooms={...} />
│   └── <TreeSection title="Mesh" accent="mesh">
│       └── <PodGroup channels={...} />     // grouped by pod
├── <ConversationView target={active}>
│   ├── <TabBar/>              one tab per open target, ctrl+w closes
│   ├── <ConversationHeader/>  topic, member count, ⋯ menu
│   ├── <MessageStream/>       unified renderer (replaces 3 sessions)
│   └── <Composer/>            single textarea, /commands, paste-attach
└── <MemberRail target={active} collapsible/>
```

`<MessageStream>` is the win — one renderer for DM, room, pod. Transport
differences live in a small adapter:

```js
// shape only
type Adapter = {
  list(): Promise<Message[]>;
  send(body): Promise<void>;
  members(): Promise<Member[]>;       // null for DMs
  capabilities: { listenAlong?: boolean, batch?: boolean };
};
```

`Message` normalizes to `{ id, ts, sender, body, kind: 'text'|'me'|'system'|'listenalong' }`.
The listen-along JSON dump visible in the current screenshot becomes
`kind:'listenalong'` rendered as a one-liner card, not raw text.

## 6. IRC-style rendering rules

- Timestamp gutter (`HH:MM`, monospace), fixed-width.
- Nick gutter, right-aligned, fixed-width up to 14 chars then ellipsis. Nick
  gets a deterministic hash color.
- Body wraps under nick column (hanging indent), weechat style.
- Consecutive messages from the same sender within 60s collapse: nick shown
  only on the first line.
- `/me` rendered italic, no colon: `* alice waves`.
- System messages (joins, leaves, listen-along start/stop) use a muted line
  color.
- A "new messages" rule fires once when scroll was at bottom and a new burst
  arrives while focus is elsewhere; `Esc` clears it.
- Day separators (`── Tue, May 6 ──`) on date change.
- Nick click → user popover (profile, browse shares, send DM, ignore).
  Replaces always-on `<UserCard>` chrome.

## 7. Composer

Single-line input that grows to ~6 lines max. Slash commands map to existing
handlers:

| Command | Effect |
|---|---|
| `/me <text>` | sends with `kind:'me'` (prefix `\x01ACTION` for slsk, native for mesh) |
| `/msg <user> <text>` | opens DM tab and sends |
| `/join <room>` | calls `rooms.join`, opens tab |
| `/part` / `/leave` | leaves current room/pod (with confirm) |
| `/close` | closes tab without leaving |
| `/batch <user,user> <text>` | replaces the modal (modal kept as fallback in `⋯` menu) |
| `/zoom s\|m\|l\|xl` | density |
| `/help` | inline cheatsheet |

Enter sends, Shift+Enter newline. No persistent send button by default; a
small ▶ icon is a hover affordance.

## 8. Tradeoffs

1. **Tabs vs grid workspace.** Today multiple panels can open side-by-side.
   Tabs are a regression for parallel monitoring. Mitigation: `⤢ split` opens
   a second pane (max 2). If 3+ panels are routine, the grid stays as an
   opt-in mode.
2. **Folding `ChatSession` / `RoomSession` / `PodChannelSession` into one.**
   Means rewriting the test files. Without this fold, density and behavior
   keep drifting across the three — the unification is the lever for
   everything else.
3. **Pure IRC default vs comfortable default.** Proposal is IRC-default
   (small, tight) with a zoom control. Alternative is Discord-default (16px,
   avatars, 1.4 line height). Pick now.
4. **Touch.** A density-first redesign is mouse/keyboard-first. Tablet users
   need the `XL` mode permanently.
5. **Polling cadence.** 2s refresh in the new compact view will be visibly
   twitchy for short DMs. Keep cadence; render through a stable virtualized
   list so paint flicker drops. A push channel for messages is out of scope
   for this redesign, worth a follow-up.

## 9. Open questions (decide before phase 1)

1. Tabs+split (proposed) vs keep the multi-card grid?
2. IRC-default density vs Discord-default density?
3. Green-light to break the existing `ChatSession` / `RoomSession` /
   `PodChannelSession` test files and rewrite against `<MessageStream>` in
   phase 3?

## 10. Plan (phased, each phase shippable on its own)

Each phase merges behind a `messaging.v2` feature flag (read from
`localStorage` key `slskd-messaging-v2`) so the current UI remains togglable
until phase 4 lands and the swap is permanent.

| # | Phase | Scope | Risk |
|---|---|---|---|
| 0 | **Contract lock** | This document; bump `localStorage` to `slskd-messaging-workspace.v2` with a one-shot migrator. | Trivial |
| 1 | **Shell + tokens** | New `<Messaging>` shell with CSS Grid, zoom CSS vars, transport accent vars, two resize handles persisted to `localStorage`. Old sidebar kept temporarily. | Low |
| 2 | **Channel tree** | Replace sidebar with `<ChannelTree>`: grouped Soulseek / Mesh sections, hover actions, right-click context menu, `Ctrl+K` quick switcher. | Medium — interactions |
| 3 | **MessageStream unification** | One renderer; adapters for chat/room/pod. Migrate `ChatSession.jsx` / `RoomSession.jsx` / `PodChannelSession` to it. Update the three test files. | Medium-high — test churn |
| 4 | **Tabs + composer** | TabBar above message view; `<Composer>` with `/commands`; `Split right` for second pane. Drop card grid. | Medium |
| 5 | **MemberRail + popover** | Collapsible rail, persisted per-target. User popover replaces inline `UserCard` chrome. | Low |
| 6 | **Polish** | Day separators, nick coloring, `/me`, listen-along card, "new messages" marker, keyboard map screen. | Low |
| 7 | **(optional)** | Push channel for messages instead of 2s polling. Out of scope for the redesign itself. | High, defer |
