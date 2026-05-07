# Security Guidelines

> **H-GLOBAL01: Logging and Telemetry Hygiene Audit**
>
> This document defines security guidelines for logging and telemetry to prevent accidental leakage of sensitive information.

## Overview

slskdN handles operator-visible activity such as file paths, IP addresses, usernames, search terms, and hashes, plus genuinely sensitive values such as passwords, API keys, private keys, and bearer tokens. This document establishes mandatory patterns for keeping real secrets out of logs without hiding the activity operators need to troubleshoot their own node.

## Core Principle

**Never log actual secrets in plain text.** Operator-visible activity belongs in operator logs. Use the formatting utilities to escape log-breaking control characters, and use explicit redaction only for credentials, API keys, private keys, passwords, bearer/session tokens, and equivalent secret material.

## Sanitization Utilities

Use `slskd.Common.Security.LoggingSanitizer` for all logging operations:

### File Paths
```csharp
// ❌ BAD - allows control characters to break log lines
_logger.LogInformation("Processing file: {Path}", "folder\r\nforged-entry.mp3");

// ✅ GOOD - preserves the path but escapes log-breaking characters
_logger.LogInformation("Processing file: {SanitizedPath}", LoggingSanitizer.SanitizeFilePath("/home/user/secret/document.pdf"));
// Result: "Processing file: /home/user/secret/document.pdf"
```

### IP Addresses
```csharp
// ❌ BAD - allows control characters to break log lines
_logger.LogInformation("Connection from: {Ip}", "192.168.1.100\r\nforged");

// ✅ GOOD - preserves endpoint context
_logger.LogInformation("Connection from: {SanitizedIp}", LoggingSanitizer.SanitizeIpAddress("192.168.1.100"));
// Result: "Connection from: 192.168.1.100"
```

### External Identifiers (Usernames, ActivityPub handles, etc.)
```csharp
// ❌ BAD - allows control characters to break log lines
_logger.LogInformation("User login: {Username}", "john\r\ndoe");

// ✅ GOOD - preserves the identifier
_logger.LogInformation("User login: {SanitizedId}", LoggingSanitizer.SanitizeExternalIdentifier("john_doe_12345"));
// Result: "User login: john_doe_12345"
```

### Sensitive Data (API keys, tokens, passwords)
```csharp
// ❌ BAD - logs secret data
_logger.LogInformation("API key: {Key}", "sk-1234567890abcdef");

// ✅ GOOD - logs redacted placeholder
_logger.LogInformation("API key: {Redacted}", LoggingSanitizer.SanitizeSensitiveData("sk-1234567890abcdef"));
// Result: "API key: [redacted-18-chars]"
```

### URLs
```csharp
// ❌ BAD - logs full URL with potential tokens
_logger.LogInformation("Request to: {Url}", "https://api.example.com/users/123?token=secret");

// ✅ GOOD - logs sanitized URL
_logger.LogInformation("Request to: {SanitizedUrl}", LoggingSanitizer.SanitizeUrl("https://api.example.com/users/123?token=secret"));
// Result: "Request to: https://api.example.com"
```

### Cryptographic Hashes
```csharp
// ❌ BAD - allows control characters to break log lines
_logger.LogInformation("File hash: {Hash}", hashWithControlCharacters);

// ✅ GOOD - preserves the full hash for troubleshooting
_logger.LogInformation("File hash: {SanitizedHash}", LoggingSanitizer.SanitizeHash(fullSha256Hash));
// Result: "File hash: <full hash>"
```

## Telemetry (Metrics) Guidelines

### Label Restrictions

**NEVER include sensitive data in metric labels:**

```csharp
// ❌ BAD - filename in metric label
Metrics.FileProcessing.WithLabels(fileName: "/secret/path/file.pdf").Inc();

// ✅ GOOD - use generic labels only
Metrics.FileProcessing.WithLabels(status: "success").Inc();
```

### Approved Label Values

- **Cardinality ≤ 10**: `status` ("success", "failure", "timeout")
- **Cardinality ≤ 100**: `method` ("GET", "POST", "PUT", "DELETE")
- **Cardinality ≤ 1000**: `endpoint` ("/api/v1/users", "/api/v1/files")

### Forbidden Labels

- ❌ File names, paths, or extensions
- ❌ IP addresses or hostnames
- ❌ User identifiers or usernames
- ❌ Full URLs or query parameters
- ❌ Cryptographic hashes or keys
- ❌ Free-form text from user input

## Implementation Patterns

### Safe Logging Context
```csharp
// Use SafeContext for structured logging with sensitive data
_logger.LogInformation("Processing request {@Context}", LoggingSanitizer.SafeContext("user", username));
// Result: { Context = "user", Id = "j***5 (13 chars)" }
```

### Batch Sanitization
```csharp
// When logging collections
var sanitizedIps = ipAddresses.Select(ip => LoggingSanitizer.SanitizeIpAddress(ip));
_logger.LogInformation("Connections from: {SanitizedIps}", string.Join(", ", sanitizedIps));
```

## Audit Checklist

Before committing code:

1. **Grep for sensitive logging patterns:**
   ```bash
   grep -r "_logger\.Log.*{" src/ | grep -E "(Path|File|Ip|Address|User|Token|Key|Hash)"
   ```

2. **Check for unsanitized metric labels:**
   ```bash
   grep -r "WithLabels.*:" src/ | grep -v "status\|method\|endpoint"
   ```

3. **Run logging hygiene tests:**
   ```bash
   dotnet test --filter LoggingHygiene
   ```

## Enforcement

- **Pre-commit hook**: `SECURITY-AUDIT.md` validation
- **CI/CD**: Automated grep checks for forbidden patterns
- **Code review**: Mandatory security review for logging changes
- **Runtime**: LoggingSanitizer enforces sanitization patterns

## Identity Separation Guidelines

**H-ID01: Identity Separation Enforcement**

Different identity types must remain strictly separated to prevent cross-contamination and credential reuse attacks.

### Identity Types

1. **Mesh Identity**: Ed25519 public/private key pairs for overlay authentication
   - Format: Base64-encoded 32-byte public keys
   - Storage: Encrypted via `IKeyStore`
   - Usage: Peer-to-peer overlay communication

2. **Soulseek Identity**: Username/password for Soulseek network access
   - Format: Alphanumeric + underscore/dot, max 30 chars
   - Storage: Configuration file (encrypted)
   - Usage: Soulseek protocol authentication

3. **Pod Identity**: Internal peer identifiers within pods
   - Format: `pod:hexhash` (sanitized) or `mesh:self`
   - Storage: Pod membership database
   - Usage: Pod communication and access control

4. **Local User Identity**: Web UI/API authentication
   - Format: Email-like or simple usernames
   - Storage: Configuration or external auth provider
   - Usage: Administrative access control

### Separation Rules

```csharp
// ❌ BAD - Bridge format leaks Soulseek identity
var podPeerId = "bridge:soulseek_username";

// ✅ GOOD - Sanitized pod identity
var podPeerId = IdentitySeparationEnforcer.SanitizePodPeerId("bridge:soulseek_username");
// Result: "pod:a1b2c3d4..." (deterministic hash)
```

```csharp
// ❌ BAD - Reusing credentials
Options.Soulseek.Username = "admin";
Options.Web.Auth.Username = "admin";

// ✅ GOOD - Distinct identities
Options.Soulseek.Username = "soulseek_user123";
Options.Web.Auth.Username = "web_admin";
```

### Implementation

Use `IdentitySeparationEnforcer` for validation:

```csharp
// Validate identity format
bool isValid = IdentitySeparationEnforcer.IsValidIdentityFormat(identity, IdentityType.Pod);

// Check for cross-contamination
bool hasLeakage = IdentitySeparationEnforcer.HasCrossContamination(identity, IdentityType.Soulseek);

// Sanitize pod peer IDs
string safePeerId = IdentitySeparationEnforcer.SanitizePodPeerId(rawPeerId);
```

### Audit Tools

```csharp
// Audit configuration
var configAudit = IdentityConfigurationAuditor.AuditConfiguration(options, logger);

// Audit pod peer IDs
var peerIdAudit = IdentitySeparationValidator.AuditPodPeerIds(peerIds, logger);

// Validate identity collection
var validation = IdentitySeparationValidator.ValidateIdentities(identities, logger);
```

## Mesh Peer Pinning — TOFU and Re-pin Workflow

slskdN uses Trust On First Use (TOFU) for mesh peer certificates: the first time your node talks to a given peer ID, that peer's SPKI is recorded in `{DataDirectory}/mesh/certificate-pins.json` and enforced on every subsequent connection via `CertificatePinManager`. After the first connection, a changed key (without a ≤30-day previous-pin grace) triggers a rejected connection and a "pin mismatch" warning.

**The TOFU window is the weak point.** An adversary in network position on your *first* contact with a peer can get their own cert pinned instead of the real one. After that, every subsequent session gets validated against the attacker's key. So TOFU is only as strong as the network conditions during the first contact.

### When you are at risk

- **Fresh install on an untrusted network** (hotel / cafe / nation-state-adjacent ISP). Every peer you meet goes through TOFU here.
- **Wiped or restored `~/.slskd`** — pin file is gone, so every peer you previously pinned goes through TOFU again. Restoring a pre-pin backup has the same effect.
- **New peer appearing in a pod you don't control** — a peer you haven't seen before is pinned on first sight.

### Pin-before-you-trust workflow

The right workflow for operators who actually care about MITM resistance:

1. **Generate pins on a known-good network.** Do first contact with your trusted peer set from a segment you control (home LAN, VPN to a known-good host). Let slskdN TOFU those peers there, then copy `{DataDirectory}/mesh/certificate-pins.json` into your deployment configuration.
2. **Commit pins to config management.** Treat `certificate-pins.json` the same as any other deployment artifact: version it, sync it across your nodes, restore it on redeploy. Do not rely on the runtime to re-TOFU the "right" peers on a fresh disk.
3. **Exchange expected pins out-of-band.** If you know another operator personally, share the SPKI fingerprint through a channel that can't be modified by the mesh (signal, in-person, signed email). On first connection, verify the logged pin against what they sent.
4. **Treat pin mismatches as incidents, not nuisances.** A "Certificate pin mismatch for peer {PeerId}" warning is the exact message you'd see during an active MITM. Investigate before clearing pins; confirm the peer actually rotated keys through a second channel before accepting.
5. **Rotate through `Previous` slot, not by wiping.** Use the rotation-with-grace-period path (old pin stays valid for ≤30 days) rather than deleting `certificate-pins.json`. Wiping the file re-opens the TOFU window for every peer at once.

### When a pin is lost

- **Do not accept the new pin silently.** Confirm out-of-band that the peer did rotate keys.
- **Do not wipe the entire pin file to resolve a single mismatch.** `RemovePeerPins(peerId)` resets only one peer's TOFU state.
- **If the entire pin file is lost** (disk failure, restore from old backup), treat every subsequent first-contact as an incident window: log the pins you collect, reconcile them against a known-good copy as soon as you get one, and investigate any divergence.

### Known limitation

There is currently no config surface for pre-seeding pins (e.g., a `mesh.peerPins` section in `slskd.yml`). The only supported path is populating `{DataDirectory}/mesh/certificate-pins.json` from a trusted generation run. Adding a declarative pin list is a follow-up.

## Related Tasks

- **H-GLOBAL01**: Logging and Telemetry Hygiene Audit (this document)
- **H-ID01**: Identity Separation Enforcement
- **H-VF01**: VirtualSoulfind Input Validation & Domain Gating
- **H-TRANSPORT01**: Mesh/DHT/Torrent/HTTP Transport Hardening
- **HARDENING-2026-04-20 H11**: Mesh TOFU re-pin workflow (this section)
- **H-MCP01**: Moderation Coverage Audit
