# Gold Star Club Design

## Overview

The **Gold Star Club** is a special pod that can automatically enroll the first
250 explicitly opted-in local daemon accounts in the slskdN network. Once
membership reaches 250, no new members can be added, even if existing members
leave. The cohort is used for realm governance bootstrap, early network testing,
and high-signal feedback.

## Requirements

1. **Explicit opt-in**: The service requires `feature.Pods: true` and the exact environment value `SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true`; without both, it does not create, publish, or auto-enroll. The environment value applies only to the local daemon/account and does not opt other users in.
2. **Limit**: Maximum 250 members
3. **One-time**: Once full, no new members can be added (even if people leave)
4. **Irrevocable leave**: A user can leave later, but leaving permanently revokes Gold Star status and cannot be undone
5. **Exclusive**: Only the first 250 users get this privilege

## Implementation

### Service: `GoldStarClubService`

Located in: `src/slskd/PodCore/GoldStarClubService.cs`

**Key Features**:
- Creates the pod on first opted-in startup (if it doesn't exist)
- Auto-joins an opted-in node when it first connects
- Enforces 250-member limit
- Records a local revocation marker when the user leaves, so an explicitly enabled auto-join does not rejoin them on restart or later
- Caches membership status to avoid repeated checks

### Pod Details

- **Pod ID**: `pod:901d57a2c1bb4e5d90d57a2c1bb4e5d0` (fixed, not random)
- **Name**: "Gold Star Club ⭐"
- **Visibility**: Listed (discoverable)
- **Tags**: `gold-star`, `first-250`, `realm-governance`, `testing`
- **Default Channel**: `gold-star-club-general`

### Auto-Join Logic

1. **On Startup**: `GoldStarClubService` runs as a `BackgroundService`
2. **Wait for Connection**: Waits up to 30 seconds for Soulseek client to connect
3. **Ensure Pod Exists**: Only after explicit opt-in, creates the pod if it doesn't exist
4. **Check Eligibility**: 
   - Checks if user is already a member
   - Requires `SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true` exactly; all other values are disabled
   - Checks if local membership was revoked by a previous leave action
   - Checks if membership count < 250
   - Checks current count again before joining (race condition protection)
5. **Join**: Adds user as a regular "member" with signed membership record

### Membership Limit Enforcement

- **Check Before Join**: Always checks current membership count before allowing join
- **Race Condition Protection**: Re-fetches members list right before joining
- **Caching**: Caches `isAcceptingMembers` status to avoid repeated DHT queries
- **One-Time**: Once limit is reached, `isAcceptingMembers` is permanently set to `false`

### Opt-In and Irrevocable Revocation

- **Before startup**: set `feature.Pods: true` and export `SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true`. Leaving the variable unset or using any value other than exact `true` keeps Gold Star dormant.
- **After joining**: leave the Gold Star Club pod from the Pods page. The server writes a local `gold-star-club.revoked` marker in the app directory so the node does not auto-join again.
- **No rejoins**: leaving is intentionally permanent. Gold Star status cannot be recovered later.

### Edge Cases Handled

1. **Race Conditions**: Multiple users trying to join simultaneously
   - Solution: Re-check membership count right before joining
   
2. **Pod Already Exists**: If pod was created manually
   - Solution: Check for existing pod before creating
   
3. **User Already Member**: If user was manually added
   - Solution: Check membership before attempting join
   
4. **Connection Delays**: Soulseek client not connected immediately
   - Solution: Wait up to 30 seconds with polling

## API

### `IGoldStarClubService`

```csharp
public interface IGoldStarClubService
{
    string GoldStarClubPodId { get; }
    int MaxMembership { get; }
    Task<bool> IsAcceptingMembersAsync(CancellationToken ct = default);
    Task<int> GetMembershipCountAsync(CancellationToken ct = default);
    Task<bool> TryAutoJoinAsync(string peerId, CancellationToken ct = default);
    Task RecordRevocationAsync(string peerId, CancellationToken ct = default);
    Task EnsurePodExistsAsync(CancellationToken ct = default);
}
```

## Configuration

Pod APIs and services are controlled by `feature.Pods`, but Gold Star itself is
not enabled by that setting alone. Creation, DHT publication, and automatic
enrollment require `SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true` exactly in the
daemon environment. The unset/default state is dormant for all users and
testers.

## Testing

Unit tests in: `tests/slskd.Tests.Unit/PodCore/GoldStarClubServiceTests.cs`

**Test Coverage**:
- Pod creation (if not exists)
- Pod existence check (if exists)
- Membership count retrieval
- Accepting members check (under/at/over limit)
- Auto-join success (under limit)
- Auto-join rejection (at limit)
- Creation and auto-join suppression when the opt-in is absent or non-`true`
- Gold Star DHT-publication suppression without exact opt-in
- Already-member check
- Race condition handling

## Future Enhancements

Potential future features:
- WebGUI indicator showing Gold Star Club status
- Badge/icon for Gold Star Club members
- Special channel or privileges for Gold Star Club members
- Statistics/metrics on Gold Star Club membership

---

**Status**: ✅ Implemented and tested
