# Upstream Intake Action Notes

Date: 2026-05-13

Scope: all `slskd/slskd` pull requests and issues updated on or after
2025-12-01. Upstream remains read-only; any work here should be implemented in
slskdN style, against the slskdN repo, without copying upstream changes
directly.

Source volume:
- Pull requests screened: 108
- Issues screened: 256

## Current Pass Results

- Built: hardened peer and mesh preview stream tickets against traversal-looking
  filenames, Unix-rooted filenames, Windows drive-letter roots on Linux, and
  malformed mesh SHA-256 expectations.
- Built: focused config compatibility warning tests for legacy `global`,
  top-level `groups`, `transfers.limits`, singular `integration`, group-level
  `limits`, retry-delay clamp warnings, and canonical-key silence.
- Verified already present: duplicate group membership validation, invalid
  blacklist regex validation, IPv4-mapped IPv6 CIDR normalization, upload
  cancel/remove endpoints, queue-position batching, clear searches, saved search
  filters, transfer batches, timeout classification, and Lidarr/Wishlist flow.
- Verified dependency posture: `npm audit --omit=dev --audit-level=moderate`
  and `dotnet list slskd.sln package --vulnerable --include-transitive` report
  no current vulnerabilities.
- Deferred intentionally: PWA, broad theming, large Browse screen redesign, and
  wider custom UX rework. They are product surface expansions, not concrete
  release-hardening gaps from this pass.

## Release-Blocking Candidates

- Harden path handling everywhere user, room, and remote filenames touch local
  paths. Cover `..`, slashes, encoded slashes, path base routing, direct file
  streaming, share aliases, and upload/download remove actions.
  Sources: issues #884, #952, #1172, #1301, #1352, #1515, #1549, #1550, #1557,
  #1592, #1596, #1600, #1601, #1615, #1616, #1696, #1717, #1719; PRs #1596,
  #1600, #1601, #1696, #1719.

- Verify the new peer and mesh streaming surfaces with adversarial tests:
  ticket expiry, authorization, local file traversal, remote path traversal,
  range handling, cancellation, bandwidth caps, and no automatic peer probing.
  Sources: issues #1658, #1659; PR #1700.

- Recheck transfer enqueue/download retry behavior under the Soulseek timeout
  pattern Bas reported. Ensure queue failure state, retry state, batch state,
  and log messages stay coherent when peers time out, reject, disconnect, or
  return zero-byte data.
  Sources: issues #959, #1510, #1593, #1603, #1607, #1609, #1631, #1636,
  #1686, #1697; PRs #1641, #1642, #1664, #1670, #1671, #1720.

- Confirm blacklist and group matching is safe and predictable for usernames,
  regex username patterns, CIDR entries, IPv4-mapped IPv6 addresses, chatroom
  callers, and runtime config changes.
  Sources: issues #1217, #1605, #1643, #1702; PRs #1578, #1579, #1606, #1713,
  #1714.

- Audit config migration compatibility after the upstream transfer key move.
  slskdN should accept legacy and current keys where practical, warn clearly,
  and avoid startup failure for common upgrade paths.
  Sources: issues #1627, #1682, #1698, #1707, #1711, #1722; PRs #1672, #1682,
  #1698, #1711.

## High-Value Near-Term Work

- Finish transfer batch UX and backend behavior as a first-class slskdN feature:
  batch lifecycle, pruning, retry counters, completed/failed retention, search
  origin, Lidarr/Wishlist origin, and API tests.
  Sources: issues #959, #1510, #1584, #1715; PRs #1594, #1664, #1670, #1671,
  #1718, #1720.

- Improve large-library browse performance and UI ergonomics. Prioritize
  lazy expansion, recursive folder download safety, multi-directory handling,
  caching only with explicit user action, and clear empty/error states.
  Sources: issues #317, #1112, #1143, #1562, #1563, #1565, #1567, #1568,
  #1617, #1721; PRs #1113, #1364, #1511, #1576.

- Bring search UX improvements into slskdN intentionally: remove completed
  searches, clear searches, save/load filters, default filter, lossless audio
  filter, and reliable Search Again payload mapping.
  Sources: issues #1505, #1618; PRs #911, #1412, #1434, #1512, #1666.

- Improve upload management and peer fairness controls: remove/cancel uploads,
  reject duplicate queued uploads, identify slow downloaders, and keep
  throttling conservative for network health.
  Sources: issues #1274, #1550, #1557, #1609, #1612, #1629; PR #1608.

- Review share database reliability under large shares, copy-on-write
  filesystems, crashes, inotify limits, glob ignores, and repeated restarts.
  Sources: issues #1050, #1265, #1416, #1468, #1545, #1625, #1661, #1663,
  #1667; PRs #1589, #1687.

- Tighten local playback/streaming UX so search, browse, local files, and mesh
  streams use one permission model and one set of visible controls.
  Sources: issues #1658, #1659; PR #1700.

## Security And Hardening

- Keep the upstream security dependency bumps tracked, but land them through
  normal slskdN dependency policy and tests.
  Sources: PRs #1590, #1613, #1614, #1630, #1635, #1654, #1655, #1676, #1680,
  #1684, #1689, #1692, #1694, #1712, #1716, #1723, #1724, #1725.

- Review API endpoints that mutate searches, transfers, config, and enqueue
  state for bad request validation, authorization, null handling, and readable
  error responses.
  Sources: issues #1404, #1554, #1633, #1636; PRs #1587, #1640, #1641, #1645,
  #1648.

- Re-audit Gluetun/VPN integration after local-control fixes: API-key redaction,
  401 behavior, resilient reconnect, listen IP/port rebinding, relay controller
  address validation, and own-share browsing while tunneled.
  Sources: issues #1646, #1660, #1668, #1677, #1681, #1721, #1726; PRs #1555,
  #1585, #1623, #1632, #1637, #1639.

- Validate startup/shutdown behavior with IPv6 unavailable, downloads queued,
  volatile mode, and state serialization failures.
  Sources: issues #1554, #1619, #1647, #1649, #1697; PRs #1580, #1648.

## Packaging And Deployment

- Review Arch/AUR, Snap, Docker, service files, and release artifact behavior
  against slskdN packaging. Keep tag-only build behavior intact.
  Sources: PRs #626, #1693, #1695, #1705, #1708, #1709.

- Verify Docker filesystem and permissions behavior for `PUID`, `PGID`, root
  upgrade compatibility, directory execute bits, and downloaded directory modes.
  Sources: issues #1626, #1650, #1653, #1706; PRs #1638, #1656, #1695, #1708,
  #1709.

- Keep FreeBSD, Windows 7, proxy environment variables, and static web asset
  notes as documentation or compatibility backlog unless they block active
  slskdN users.
  Sources: issues #905, #1199, #1435, #1683.

## UI And Product Polish

- Consider PWA support only after security review of caching, auth, and update
  semantics.
  Sources: issue #1241; PR #1577.

- Improve Files/System/Logs polish: download button clarity, sortable system
  files, dynamic log limits, transfer detail popups, Safari autofill prevention,
  and special-user input behavior.
  Sources: PRs #448, #1552, #1581, #1582, #1604, #1719.

- Review theming work for slskdN compatibility and accessibility before adding
  broad custom theme support.
  Sources: PR #1518; issue #1620.

- Add user-facing affordances for ignoring users, quick browsing uploaders,
  useful upload hyperlinks, and queue position visibility where they do not
  increase network pressure.
  Sources: issues #921, #1565, #1652; PR #1516.

## Runtime Fork Watchlist

- Evaluate Soulseek.NET runtime updates in isolation: minor version changes,
  agent name behavior, wishlist interval support, transfer timeout semantics,
  and search response delivery behavior. Keep slskdN network-health limits in
  front of any runtime change.
  Sources: PRs #1610, #1674, #1691; issues #1593, #1598, #1631.

- Investigate whether peer bans or failed downloads correlate with client
  version, agent naming, listen IP/port behavior, or upload queue behavior.
  This should use controlled kspls0 observations and synthetic runtime tests,
  not broad network probing.
  Sources: issues #1598, #1603, #1631, #1681; PRs #1610, #1674, #1691.

## Already Covered Locally, Keep Verifying

- Lidarr/Wishlist acquisition exists in slskdN; keep testing dedupe, safe
  auto-download, native wishlist search scope, and post-download import.
  Sources: issue #1222 plus slskdN-specific work.

- Liked/hated interest broadcast options appear present locally; verify config,
  docs, and runtime behavior.
  Sources: issues #1678; PR #1679.

- Username regex blacklist patterns appear present locally; keep tests around
  regex matching and blacklist group behavior.
  Sources: issue #1702; PRs #1699, #1713.

- Docker `PUID`/`PGID` support appears present locally; keep package and
  container tests around upgrade and root compatibility.
  Sources: PR #1695.

## Lower-Priority Holding Area

- Chat room search should be case-insensitive and handle rooms with slashes
  consistently.
  Sources: issues #1172, #1592; PR #1556.

- Metrics should include conservative transfer enqueue and incoming search
  throttling counters.
  Sources: PRs #1588, #1608.

- Album consensus, track-length anomalies, and lossless filtering are useful
  music-quality features but should not block transfer correctness.
  Sources: issues #1505, #1569, #1607.

- Cache browse responses only with user control and expiry. Avoid automatic
  broad browse pressure.
  Sources: issues #1568, #317.

- Config/script documentation should include the script JSON schema and any
  slskdN-specific data fields.
  Sources: issue #1404; PR #1688.

## Source Inventory

Pull requests screened:
#2, #6, #448, #626, #667, #911, #1065, #1113, #1251, #1364, #1383, #1412, #1434, #1477, #1503, #1511, #1512, #1516, #1518, #1520, #1547, #1552, #1553, #1555, #1556, #1570, #1576, #1577, #1578, #1579, #1580, #1581, #1582, #1583, #1585, #1586, #1587, #1588, #1589, #1590, #1591, #1594, #1596, #1597, #1600, #1601, #1604, #1606, #1608, #1610, #1613, #1614, #1623, #1630, #1632, #1635, #1637, #1638, #1639, #1640, #1641, #1642, #1644, #1645, #1648, #1654, #1655, #1656, #1664, #1666, #1670, #1671, #1672, #1674, #1675, #1676, #1679, #1680, #1682, #1684, #1687, #1688, #1689, #1690, #1691, #1692, #1693, #1694, #1695, #1696, #1698, #1699, #1700, #1704, #1705, #1708, #1709, #1711, #1712, #1713, #1714, #1716, #1718, #1719, #1720, #1723, #1724, #1725.

Issues screened:
#1, #74, #89, #107, #180, #193, #197, #222, #317, #328, #345, #401, #405, #411, #419, #424, #426, #427, #451, #470, #475, #488, #495, #501, #505, #529, #533, #542, #546, #573, #590, #593, #607, #608, #629, #639, #646, #650, #652, #662, #690, #704, #706, #714, #750, #765, #767, #773, #811, #814, #855, #884, #895, #898, #904, #905, #908, #912, #914, #921, #927, #933, #952, #959, #960, #965, #966, #968, #969, #989, #997, #1004, #1011, #1027, #1034, #1043, #1046, #1050, #1074, #1075, #1083, #1087, #1093, #1094, #1112, #1118, #1126, #1127, #1131, #1143, #1146, #1147, #1153, #1160, #1162, #1163, #1164, #1166, #1171, #1172, #1173, #1175, #1181, #1183, #1188, #1189, #1192, #1193, #1198, #1199, #1202, #1217, #1222, #1231, #1239, #1241, #1254, #1265, #1266, #1267, #1268, #1269, #1274, #1275, #1276, #1280, #1291, #1293, #1300, #1301, #1305, #1306, #1316, #1327, #1332, #1333, #1336, #1339, #1341, #1346, #1347, #1352, #1353, #1354, #1358, #1360, #1362, #1366, #1367, #1370, #1375, #1378, #1400, #1401, #1402, #1404, #1405, #1416, #1429, #1431, #1432, #1435, #1437, #1442, #1450, #1454, #1455, #1466, #1468, #1476, #1478, #1499, #1502, #1505, #1510, #1515, #1532, #1533, #1535, #1545, #1548, #1549, #1550, #1551, #1554, #1557, #1558, #1559, #1560, #1561, #1562, #1563, #1564, #1565, #1566, #1567, #1568, #1569, #1584, #1592, #1593, #1595, #1598, #1599, #1603, #1605, #1607, #1609, #1611, #1612, #1615, #1616, #1617, #1618, #1619, #1620, #1621, #1625, #1626, #1627, #1628, #1629, #1631, #1633, #1636, #1643, #1646, #1647, #1649, #1650, #1651, #1652, #1653, #1657, #1658, #1659, #1660, #1661, #1662, #1663, #1667, #1668, #1677, #1678, #1681, #1683, #1686, #1697, #1702, #1706, #1707, #1715, #1717, #1721, #1722, #1726.
