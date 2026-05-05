#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

scan() {
  local title="$1"
  local pattern="$2"
  shift 2

  printf '\n## %s\n' "$title"
  rg -n --with-filename --pcre2 --hidden --glob '!.git/**' "$pattern" "$@" || true
}

scan_multiline() {
  local title="$1"
  local pattern="$2"
  shift 2

  printf '\n## %s\n' "$title"
  rg -n -U --with-filename --pcre2 --hidden --glob '!.git/**' "$pattern" "$@" || true
}

scan_protocol_scalar_guards() {
  printf '\n## Protocol scalar constructor guard candidates\n'

  find src/Messaging -name '*.cs' -print0 | sort -z | xargs -0 perl -0ne '
    while (/^[ \t]*public[ \t]+(?:(?:[A-Za-z0-9_<>.]+[ \t]+)?[A-Za-z0-9_]+)[ \t]*\(([^)]*)\)/mg) {
      my $declaration = $&;
      my $arguments = $1;
      my $start = $-[0];
      next unless $declaration =~ /^[ \t]*public[ \t]+MessageBuilder[ \t]+Write(?:Byte|Bytes|Integer|Long|String)[ \t]*\(/
        || $arguments =~ /\b(?:int|long|UserPresence|TransferDirection|MessageCode\.Server)\b/;

      my $line = 1 + (substr($_, 0, $start) =~ tr/\n//);
      $declaration =~ s/\s+/ /g;
      $declaration =~ s/^\s+|\s+$//g;
      print "$ARGV:$line:$declaration\n";
    }
  '
}

printf '# slskNet.Runtime bug council candidate scan\n'
printf '# Generated: %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')"

scan "Mutable public byte arrays and array properties" \
  'public [^;\n=]*\[\][^{;\n]*(\{|=>|;)|\bbyte\[\]\s+[A-Z][A-Za-z0-9_]*\s*\{' \
  src tests/Soulseek.Tests.Unit

scan_multiline "Constructors accepting mutable collections or params arrays" \
  '\b(public|internal) [A-Za-z0-9_]+\s*\([^)]*(IEnumerable<|IReadOnlyCollection<|ICollection<|IList<|List<|Dictionary<|HashSet<|params )' \
  src

scan "Value equality and hash-code comparisons" \
  'GetHashCode\(\)\s*==|Equals\([^)]*GetHashCode|operator ==|operator !=|public bool Equals\(' \
  src

scan "Non-idempotent task completion candidates" \
  '\.Set(Result|Exception|Canceled)\(' \
  src

scan "Task, cancellation, timer, and semaphore lifecycle candidates" \
  'TaskCompletionSource|CancellationTokenSource|SemaphoreSlim|Timer|Task\.WhenAny|ContinueWith|async void|Register\(' \
  src

scan "Lifecycle task completion and race candidates" \
  'TaskCompletionSource|Task\.WhenAny|ContinueWith|async void' \
  src

scan "Lifecycle cancellation registration candidates" \
  'CancellationTokenSource|CreateLinkedTokenSource|Register\(' \
  src

scan "Lifecycle timer and semaphore candidates" \
  'SemaphoreSlim|System\.Timers\.Timer|SystemTimer|Timer\(|\.Reset\(\)|\.Stop\(\)|\.Start\(\)' \
  src

scan "Lifecycle fire-and-forget async misuse candidates" \
  '_ = .*\.ConfigureAwait\(false\);|^[[:space:]]*[A-Za-z0-9_.]+Async\([^)]*\)\.ConfigureAwait\(false\);' \
  src

scan "Protocol count and length allocation candidates" \
  'ReadInteger\(\)|ReadLong\(\)|ReadBytes\([^)]*\)|new byte\[[^]]+\]|for \(int i = 0; i < [^;]+; i\+\+\)|while \(' \
  src/Messaging src/Network

scan "Protocol counted collection loops" \
  'ProtocolCountReader\.ReadCount|for \(int i = 0; i < [A-Za-z0-9_]*(Count|count); i\+\+\)|for \(int j = 0; j < [A-Za-z0-9_]*(Count|count); j\+\+\)|ReadFiles\([^)]*count' \
  src/Messaging src/Network

scan "Protocol length-prefixed reads and payload allocations" \
  'ReadStringAndEncoding|ReadBytes\(int count\)|var length = ReadInteger\(\)|var pictureLen = reader\.ReadInteger\(\)|new byte\[8 \+ length\]|new byte\[checked\(\(int\)length\)\]|new byte\[FrameLengthBytes \+ payload\.Length\]|new byte\[4 \+ input\.Length\]|new byte\[input\.Length - 4\]' \
  src/Messaging src/Network

scan "Protocol compression boundary candidates" \
  'Decompress\(|MaximumDecompressedPayloadLength|BoundedMemoryStream|new byte\[bufsize\]|new byte\[len\]|window = new byte|pending_buf = new byte' \
  src/Messaging

scan "Protocol scalar emission candidates" \
  'Write(Integer|Long|Byte|String|Bytes)\(' \
  src/Messaging

scan_protocol_scalar_guards

scan "Resolver output and raw stream candidates" \
  'Resolver|Raw.*Response|Stream|WriteAsync\([^)]*Stream|ToByteArray\(\)' \
  src/Messaging src/Options src/SoulseekClient.cs

scan "Resolver delegate surface candidates" \
  'SearchResponseResolver|BrowseResponseResolver|DirectoryContentsResolver|UserInfoResolver|PlaceInQueueResolver|EnqueueDownload' \
  src/Options src/SoulseekClient.cs src/Messaging/Handlers/PeerMessageHandler.cs

scan "Peer resolver dispatch candidates" \
  'Options\.(SearchResponseResolver|BrowseResponseResolver|DirectoryContentsResolver|UserInfoResolver|PlaceInQueueResolver)|WriteRaw(Search|Browse)ResponseAsync|Raw(Search|Browse)Response|TrySendPlaceInQueueAsync|new PlaceInQueueResponse' \
  src/Messaging/Handlers/PeerMessageHandler.cs

scan "Transfer stream factory candidates" \
  'Func<Task<Stream>>|Func<long, Task<Stream>>|outputStreamFactory|inputStreamFactory|Dispose(Input|Output)StreamOnCompletion|Seek(Input|Output)StreamAutomatically' \
  src/SoulseekClient.cs src/Options/TransferOptions.cs src/Network/Tcp

scan "Example Web API path, request, and lifecycle candidates" \
  'Path|File\.|Directory\.|Request|CancellationTokenSource|Stream|IFormFile|FromBody|ActionResult|BadRequest|Ok\(' \
  examples/Web/api tests/Soulseek.Tests.Unit/WebApi*

scan "Security-sensitive material candidates" \
  'PRIVATE KEY|gh[pousr]_|xox[baprs]-|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)' \
  .

printf '\n# End of candidate scan. Every hit must be ledgered as Fixed, Existing guard, False positive, or Out of scope before a council sweep is closed.\n'
