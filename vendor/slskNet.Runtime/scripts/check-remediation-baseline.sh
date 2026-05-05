#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

failures=0

pass() {
  printf 'PASS %s\n' "$1"
}

fail() {
  printf 'FAIL %s\n' "$1" >&2
  failures=$((failures + 1))
}

require_file() {
  local path="$1"
  local label="$2"

  if [[ -f "$path" ]]; then
    pass "$label"
  else
    fail "$label: missing $path"
  fi
}

require_pattern() {
  local pattern="$1"
  local path="$2"
  local label="$3"

  if rg -n -U --pcre2 --hidden --glob '!.git/**' "$pattern" "$path" >/dev/null; then
    pass "$label"
  else
    fail "$label"
  fi
}

require_absent_pattern() {
  local pattern="$1"
  local path="$2"
  local label="$3"

  if rg -n -U --pcre2 --hidden --glob '!.git/**' "$pattern" "$path" >/tmp/slsknet-runtime-remediation-hit.$$ 2>/dev/null; then
    fail "$label"
    sed 's/^/  /' /tmp/slsknet-runtime-remediation-hit.$$ >&2
  else
    pass "$label"
  fi

  rm -f /tmp/slsknet-runtime-remediation-hit.$$
}

require_file "docs/dev/bug-burndown-ledger.md" "bug burndown ledger exists"
require_file "scripts/check-remediation-baseline.sh" "remediation baseline script exists"

require_pattern "ProtocolCountReader" "src/Messaging/Messages" "protocol parsers use centralized count reader"
require_pattern "ReadValidatedCount" "src/Messaging/Messages/Server/ProtocolCountReader.cs" "protocol count validation is centralized"
require_pattern "count < 0" "src/Messaging/Messages/Server/ProtocolCountReader.cs" "protocol count reader rejects negative counts"
require_pattern "count > maximumPossibleCount" "src/Messaging/Messages/Server/ProtocolCountReader.cs" "protocol count reader rejects impossible counts"
require_pattern "ValidateMatchingCount" "src/Messaging/Messages" "parallel protocol collection counts are matched"
require_pattern "ValidateNonNegativeCount" "src/Messaging/Messages/Server/RoomListResponseFactory.cs" "room list user counts reject negative values"
require_pattern "Invalid file size" "src/Messaging/MessageReaderExtensions.cs" "file parsers reject invalid negative sizes"
require_pattern "Invalid transfer file size" "src/Messaging/Messages/Peer" "transfer parsers reject invalid negative sizes"
require_pattern "ValidatePort" "src/Messaging/Messages/Server" "server endpoint parsers validate ports"
require_pattern "ValidateAdvertisedPort" "src/Messaging/Messages/Server" "obfuscated endpoint metadata validates advertised ports"
require_pattern "ValidateNonNegative" "src/Messaging/Messages" "protocol scalar parsers reject negative counters"
require_pattern "ValidateDefinedEnum" "src/Messaging/Messages" "protocol enum values are validated"
require_pattern "ValidateBooleanFlag" "src/Messaging/Messages" "protocol boolean flags are validated"
require_pattern "ValidateBooleanFlag" "src/Messaging/Messages/Server" "server protocol boolean flags are validated"
require_pattern "ValidateBooleanFlag" "src/Messaging/Messages/Peer" "peer protocol boolean flags are validated"
require_pattern "branch level" "src/Messaging/Messages/Distributed/DistributedBranchLevel.cs" "distributed branch level rejects invalid scalars"
require_pattern "child depth" "src/Messaging/Messages/Distributed/DistributedChildDepth.cs" "distributed child depth rejects invalid scalars"
require_pattern "ProtocolCountHardeningTests" "tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolCountHardeningTests.cs" "protocol count regression tests are registered"
require_pattern "ProtocolScalarHardeningTests" "tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs" "protocol scalar regression tests are registered"
require_pattern "ArgumentOutOfRangeException" "src/Messaging/Messages/Server/SetSharedCountsCommand.cs" "shared count commands reject invalid emitted counters"
require_pattern "ArgumentOutOfRangeException" "src/Messaging/Messages/Server/SendUploadSpeedCommand.cs" "upload speed commands reject invalid emitted counters"

require_pattern "ValidateMessageLength" "src/Network" "message frame length validation is wired"
require_pattern "ValidateInitMessageLength" "src/Network" "initialization frame length validation is wired"
require_pattern "MaxMessageLength" "src/Network/MessageFrameValidator.cs" "message frames are bounded"
require_pattern "MaxInitMessageLength" "src/Network/MessageFrameValidator.cs" "initialization frames are bounded"
require_pattern "MessageFrameValidatorTests" "tests/Soulseek.Tests.Unit/Network/MessageFrameValidatorTests.cs" "frame validation regression tests are registered"
require_pattern "MaximumBufferedReadLength" "src/Network/Tcp/Connection.cs" "buffered reads have an allocation limit"

require_pattern "TrySet(Result|Exception|Canceled)" "src" "runtime task completion uses idempotent completion APIs"
require_absent_pattern "\.Set(Exception|Result|Canceled)\(" "src" "runtime source avoids non-idempotent task completion"
require_pattern "CreateLinkedTokenSource" "src/SoulseekClient.cs" "transfer races use linked cancellation"
require_pattern "Task\.WhenAny\\([\\s\\S]*disconnectedTaskCancellationSource\.Task" "src/SoulseekClient.cs" "transfer races include disconnect task"
require_pattern "RemoteTaskCompletionSource\.TrySetException" "src/SoulseekClient.cs" "remote transfer failures complete idempotently"

require_pattern "return false" "src/Ed25519PeerDescriptorSigner.cs" "peer descriptor verification fails closed"
require_pattern "catch[\\s\\S]*return false" "src/Ed25519PeerDescriptorSigner.cs" "peer descriptor verifier handles malformed signatures"
require_pattern "discovery hints rather than authorization decisions" "README.md" "peer capabilities are documented as non-authorization hints"

require_pattern "GetFullPathInsideRoot" "examples/Web/api/Extensions.cs" "example Web API has root containment helper"
require_pattern "GetSafeOutputPath" "examples/Web/api/Extensions.cs" "example Web API has safe output helper"
require_pattern "IsPathInsideRoot" "examples/Web/api/Extensions.cs" "example Web API checks normalized root containment"
require_pattern "GetLocalFileStream\\(request\\.Filename, OutputDirectory\\)" "examples/Web/api/Controllers/TransfersController.cs" "example download endpoint defers output stream creation"
require_pattern "CancellationTokenSource\\?\\.Dispose" "examples/Web/api/Trackers/TransferTracker.cs" "example transfer tracker disposes removed cancellation sources"
require_pattern "DisposeUntrackedCancellationTokenSource" "examples/Web/api/Controllers/TransfersController.cs" "example download endpoint disposes untracked cancellation sources"
require_pattern "ReferenceEquals\\(record\\.CancellationTokenSource, cancellationTokenSource\\)" "examples/Web/api/Trackers/TransferTracker.cs" "example transfer tracker disposes replaced cancellation sources"
require_pattern "finally" "examples/Web/api/Startup.cs" "example upload task disposes cancellation source on failure"
require_pattern "WebApiPathSecurityTests" "tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs" "example path security tests are registered"
require_pattern "WebApiTransferTests" "tests/Soulseek.Tests.Unit/WebApiTransferTests.cs" "example transfer lifecycle tests are registered"
require_pattern "WebApiRequestTests" "tests/Soulseek.Tests.Unit/WebApiRequestTests.cs" "example request validation tests are registered"
require_pattern "messageLimit < 1" "examples/Web/api/Trackers/RoomTracker.cs" "example room tracker rejects invalid message limits"
require_pattern "room.Messages \\?\\?=" "examples/Web/api/Trackers/RoomTracker.cs" "example room tracker normalizes missing message lists"
require_pattern "room.Users \\?\\?=" "examples/Web/api/Trackers/RoomTracker.cs" "example room tracker normalizes missing user lists"
require_pattern "progress == null" "examples/Web/api/Trackers/BrowseTracker.cs" "example browse tracker rejects null progress"
require_pattern "message == null" "examples/Web/api/Trackers/ConversationTracker.cs" "example conversation tracker rejects null messages"
require_pattern "RoomTracker_Rejects_Invalid_Message_Limit" "tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs" "example tracker validation tests are registered"
require_pattern "TryNormalizeSearchRequest" "examples/Web/api/Controllers/SearchesController.cs" "example search endpoint validates request bodies"
require_pattern "Search timeout must be greater than or equal to one" "examples/Web/api/Controllers/SearchesController.cs" "example search endpoint validates option ranges"
require_pattern "Request body is required" "examples/Web/api/Controllers/ServerController.cs" "example connect endpoint validates request bodies"
require_pattern "Port must be between" "examples/Web/api/Controllers/ServerController.cs" "example connect endpoint validates port ranges"
require_pattern "Message is required" "examples/Web/api/Controllers/RoomsController.cs" "example room write endpoints validate messages"
require_pattern "Username is required" "examples/Web/api/Controllers/ConversationsController.cs" "example conversation write endpoint validates username"
require_pattern "File.Exists\\(PicturePath\\)" "examples/Web/api/Startup.cs" "example user info resolver tolerates missing sample image"
require_pattern "GetAvailablePort" "tests/Soulseek.Tests.Unit/Client/ReconfigureOptionsAsyncTests.cs" "listener reconfiguration tests use dynamic ports"
require_pattern "GetAvailablePort" "tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs" "listener connection tests use dynamic ports"

require_pattern "searchTimeout < 1" "src/Options/SearchOptions.cs" "search options reject invalid timeout values"
require_pattern "responseTimeout < 1" "src/Options/BrowseOptions.cs" "browse options reject invalid timeout values"
require_pattern "connectTimeout < 0" "src/Options/ConnectionOptions.cs" "connection options reject invalid connect timeout values"
require_pattern "readBufferSize < 1" "src/Options/ConnectionOptions.cs" "connection options reject invalid buffer values"
require_pattern "inactivityTimeout < -1" "src/Options/ConnectionOptions.cs" "connection options constrain inactivity timeout values"
require_pattern "DomainModelValidationTests" "tests/Soulseek.Tests.Unit/DomainModelValidationTests.cs" "domain model validation tests are registered"
require_pattern "size < 0" "src/File.cs" "file domain model rejects invalid sizes"
require_pattern "responseCount < 0" "src/Search.cs" "search domain model rejects invalid counters"
require_pattern "uploadSpeed < 0" "src/SearchResponse.cs" "search response domain model rejects invalid peer metadata"
require_pattern "uploadSlots < 0" "src/UserInfo.cs" "user info domain model rejects invalid peer metadata"
require_pattern "Enum\\.IsDefined\\(typeof\\(UserPresence\\)" "src/UserData.cs" "user data domain model validates presence values"
require_pattern "Enum\\.IsDefined\\(typeof\\(UserPresence\\)" "src/UserStatus.cs" "user status domain model validates presence values"
require_pattern "averageSpeed < 0" "src/UserStatistics.cs" "user statistics domain model rejects invalid peer metadata"
require_pattern "Enum\\.IsDefined\\(typeof\\(TransferDirection\\)" "src/Transfer.cs" "transfer domain model validates direction values"
require_pattern "userCount < 0" "src/RoomInfo.cs" "room info domain model rejects invalid user counts"
require_pattern "branchLevel < 0" "src/DistributedNetworkInfo.cs" "distributed network info rejects invalid topology counters"
require_pattern "bytesTransferred < 0" "src/EventArgs/BrowseProgressUpdatedEventArgs.cs" "browse progress events reject negative transferred byte counts"
require_pattern "bytesTransferred > size" "src/EventArgs/BrowseProgressUpdatedEventArgs.cs" "browse progress events reject over-complete progress"
require_pattern "Enum\\.IsDefined\\(typeof\\(SearchScopeType\\)" "src/SearchScope.cs" "search scopes validate defined scope types"
require_pattern "Enum\\.IsDefined\\(typeof\\(FileAttributeType\\)" "src/FileAttribute.cs" "file attributes validate defined types"
require_pattern "value < 0" "src/FileAttribute.cs" "file attributes reject negative values"
require_pattern "ValidateDefinedEnum\\(type, \"file attribute type\"" "src/Messaging/MessageReaderExtensions.cs" "file parsers validate attribute types"
require_pattern "ValidateNonNegative\\(value, \"file attribute value\"" "src/Messaging/MessageReaderExtensions.cs" "file parsers validate attribute values"
require_pattern "TransferStates ValidStates" "src/Transfer.cs" "transfers validate defined state flags"
require_pattern "placeInQueue < 0" "src/Messaging/Messages/Peer/PlaceInQueueResponse.cs" "peer queue responses reject negative positions"
require_pattern "Enum\\.IsDefined\\(typeof\\(DiagnosticLevel\\)" "src/Diagnostics/DiagnosticEventArgs.cs" "diagnostic events validate defined levels"
require_pattern "SoulseekClientStates ValidStates" "src/EventArgs/SoulseekClientStateChangedEventArgs.cs" "client state events validate defined flags"
require_pattern "SearchStates ValidStates" "src/EventArgs/SearchStateChangedEventArgs.cs" "search state events validate defined flags"
require_pattern "TransferStates ValidStates" "src/EventArgs/TransferStateChangedEventArgs.cs" "transfer state events validate defined flags"
require_pattern "Math\\.Max\\(0, \\(Size \\?\\? 0\\) - BytesTransferred\\)" "src/TransferInternal.cs" "internal transfer remaining bytes are clamped"
require_pattern "Size\\.Value > 0" "src/TransferInternal.cs" "internal transfer percent avoids zero-size division"
require_pattern "startOffset < 0" "src/SoulseekClient.cs" "uploads reject negative peer start offsets"
require_pattern "StartOffset_Rejects_Negative_Values" "tests/Soulseek.Tests.Unit/TransferInternalTests.cs" "internal transfer start offset validation tests are registered"
require_pattern "Peer_Sends_Negative_StartOffset" "tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs" "upload negative start offset regression test is registered"
require_pattern "intervalOverride\\.Value <= TimeSpan\\.Zero" "src/WishlistSearchSchedulerOptions.cs" "wishlist scheduler rejects invalid override intervals"
require_pattern "minimumInterval\\.Value <= TimeSpan\\.Zero" "src/WishlistSearchSchedulerOptions.cs" "wishlist scheduler rejects invalid minimum intervals"
require_pattern "Wishlist_Scheduler_Options_Reject_Non_Positive_Intervals" "tests/Soulseek.Tests.Unit/WishlistSearchSchedulerTests.cs" "wishlist scheduler interval validation tests are registered"
require_pattern "MaximumUploadSpeed < 1" "src/Options/SoulseekClientOptions.cs" "client options reject invalid upload speed capacity"
require_pattern "MaximumDownloadSpeed < 1" "src/Options/SoulseekClientOptions.cs" "client options reject invalid download speed capacity"
require_pattern "MaximumUploadSpeed\\.HasValue && MaximumUploadSpeed\\.Value < 1" "src/Options/SoulseekClientOptionsPatch.cs" "client option patches reject invalid upload speed capacity"
require_pattern "Throws_If_Maximum_Speed_Is_Less_Than_One" "tests/Soulseek.Tests.Unit/Options" "client option speed validation tests are registered"

require_pattern "<PackageId>slskNet\.Runtime</PackageId>" "src/Soulseek.csproj" "package id uses fork branding"
require_pattern "snapetech/slskNet\.Runtime" "src/Soulseek.csproj" "package metadata points to fork repository"
require_pattern "^# slskNet\.Runtime" "README.md" "README uses runtime branding"
require_pattern "mcr\.microsoft\.com/dotnet/sdk:8\.0" ".circleci/config.yml" "CI uses current runtime SDK image"
require_absent_pattern "\"name\"\\s*:" "package.json" "repo root does not define Node package metadata"

require_pattern "bash scripts/check-remediation-baseline\.sh" "docs/dev/bug-burndown-ledger.md" "ledger references remediation baseline command"
require_pattern "RT-001" "docs/dev/bug-burndown-ledger.md" "ledger contains finding registry"

secret_pattern='-----BEGIN (RSA |DSA |EC |OPENSSH |PGP )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{36,}|xox[baprs]-[A-Za-z0-9-]{20,}|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)["'\'']?\s*[:=]\s*["'\''][A-Za-z0-9_./+=-]{24,}["'\'']'
require_absent_pattern "$secret_pattern" "." "tracked text files do not contain high-confidence secret patterns"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d remediation baseline check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll remediation baseline checks passed.\n'
