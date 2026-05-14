# Self-Healing Messaging Design (Slack-Like Use Case)

**Status**: DESIGN - Future Resilience Layer  
**Created**: December 12, 2025  
**Priority**: 🟡 MEDIUM (after Slack-grade messaging baseline)

> **Project Note**: This is a fork of [slskd](https://github.com/slskd/slskd). See [../README.md](../README.md#acknowledgments) for attribution.

---

This document defines additional behavior to make messaging **self-healing** in a Slack-like deployment:

- Channel-level redundancy and failover
- Health-aware fanout for cross-pod channels
- Degradation behavior when parts of the network are unhealthy

**It builds on:**
- `docs/health-routing-design.md`
- `docs/replication-policy-design.md`
- `docs/slack-grade-messaging-design.md`
- `docs/realm-design.md`
- `docs/federation-security-hardening.md`

---

## 1. Channel-Level Redundancy (Optional Feature)

We define an optional "resilient channel" mode:

**Certain channels (e.g., org-critical channels):**
- `#general`, `#incidents`, `#sre`, etc.
- Can be marked as **resilient** with a defined **replica set** of pods

### 1.1 ResilientChannelConfig

For each such channel:

```yaml
resilient_channel:
  enabled: true
  org_channel_id: "org:1234#general"
  replicas:
    - pod_id_A
    - pod_id_B
  replication_class: "SmallBlob" # channel metadata, not huge attachments
```

**`replicas`:**
- List of pods that act as co-hosts for that channel

**`replication_class`:**
- Defines allowed replication level (small metadata + messages)

---

### 1.2 Data Model

**Channel messages are:**

**Stored on:**
- The primary pod (where message originated)
- Optionally mirrored to one or more replicas in **compact form**:
  - Message text and metadata
  - Minimal necessary indexing for read/search

**Attachments:**
- ❌ Not replicated by default
- ⚠️ For critical channels, a future extension MAY allow:
  - Bounded mirroring of small attachments, still under strict quotas

---

### 1.3 Failure Modes

**If the primary pod for a resilient channel becomes unavailable:**

**Clients connected to replica pods can:**
- ✅ Continue reading channel history from replicas
- ✅ Optionally send new messages via replicas if allowed by configuration

**Once the primary recovers:**
- ✅ A reconciliation process (bi-directional diff) ensures:
  - Messages created during outage are synchronized
  - Conflicts are resolved via timestamps and tie-breakers

---

## 2. Health-Aware Fanout

Cross-pod channel fanout (for org-shared channels or federated channels) is mediated by `HealthManager`:

### 2.1 Message Fanout Paths

**When a message in an org-shared channel must be delivered to other pods:**

**The sending pod chooses fanout paths based on:**
- ✅ `HealthScore` of candidate peers (pods/relays)
- ✅ Realm and peering policies

**Delivery strategy:**
- ✅ Try the healthiest peers first
- ✅ If a direct connection is unhealthy, try:
  - Alternate relays or bridging pods
- ⚠️ After repeated failures:
  - Mark that peer/path as degraded
  - Back off and log

---

### 2.2 Partial Delivery & Degradation

**If some pods in the org are unreachable:**

**The system:**
- ✅ Delivers messages to reachable pods
- ✅ Marks unreachable ones as "behind" in internal state
- ✅ Once connectivity is restored:
  - Replays backlog from the durable log

**User-facing behavior:**

**Clients see:**
- ✅ Messages flowing normally in reachable pods
- ⚠️ Possibly an indicator that:
  - "Some org peers are temporarily unreachable; messages will sync when they return," if UI chooses to show it

**No global "stop the world" behavior is allowed; outages are localized.**

---

## 3. Self-Healing Against Data Loss

### 3.1 Limited Replication for Critical Data

As per `replication-policy-design.md`, replication for messaging is:

**Default: None**

**Optional for:**
- Resilient channels
- Certain system-critical logs (incidents, governance logs if applicable)

**Objects eligible for replication:**
- Channel message logs (text and metadata) for designated resilient channels
- Channel configuration & ACLs

**Everything is:**
- ✅ Small or bounded in size
- ✅ Filtered through MCP for compliance (no disallowed content)

---

### 3.2 Recovery Scenarios

**When a pod suffers:**

**Local storage failure:**
- ✅ If a resilient channel is configured:
  - It can restore from replicas, subject to:
    - Org policies
    - Explicit admin action

**Local corruption of indexes:**
- ✅ Pod can rebuild search and indexes from replicated message logs

**This is deliberate: we only give "self-healing" to channels and data explicitly marked as critical and replication-eligible.**

---

## 4. Degraded Modes

**When parts of the system are unhealthy:**

**Unhealthy pods:**
- ⚠️ HealthScore drops below threshold
- Pods may:
  - Temporarily stop acting as replicas
  - Stop serving as fanout relays

**Unhealthy backends:**
- ⚠️ Metadata/LLM providers failing:
  - Messaging stack continues with degraded functionality:
    - No recommendations, weaker moderation, etc.
  - But core send/receive remains available

**Unhealthy federation:**
- ⚠️ If AP federation to some instances is failing:
  - Local channels remain usable
  - Federation is retried via backoff; not blocking local operation

---

## 5. Configuration & Safety

### 5.1 Opt-In Only

**Resilient channels and message replication:**

**MUST be opt-in:**
- ✅ Defined explicitly in channel or org configuration
- ❌ Never enabled globally by default

---

### 5.2 Quotas & Limits

**Per pod:**
- Max number of resilient channels
- Max total replicated message volume
- Rate limits on replication traffic

**Violations:**
- ⚠️ If replication exceeds limits:
  - Replication is throttled
  - Admins are notified (via logs / alerts)

---

### 5.3 Security & Privacy

**Replicated data:**
- ✅ Must be encrypted in transit and at rest (if encryption is available)
- ✅ May be additionally:
  - Encrypted with org-level keys so only authorized pods can read message logs
- ✅ Access must respect:
  - Channel ACLs
  - Org policies

**Self-healing is always constrained by:**
- ✅ The same hardening requirements as replication and federation:
  - No generic storage network
  - No accidental leakage of private content outside designated replicas

---

## Related Documents

- `docs/slack-grade-messaging-design.md` - Real-time messaging semantics
- `docs/health-routing-design.md` - Health scoring and routing
- `docs/replication-policy-design.md` - Replication constraints
- `docs/realm-design.md` - Realm isolation
- `docs/federation-security-hardening.md` - Security requirements
- `docs/pod-f1000-social-hub-design.md` - ChatModule, ForumModule baseline
