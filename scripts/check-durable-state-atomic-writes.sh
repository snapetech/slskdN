#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

reject_pattern() {
  local file="$1"
  local pattern="$2"

  if grep -Eq -- "$pattern" "$repo_root/$file"; then
    printf '%s contains non-atomic durable state write: %s\n' "$file" "$pattern" >&2
    failed=1
  fi
}

reject_pattern src/slskd/Jobs/Manifests/JobManifestService.cs 'File\.WriteAllTextAsync'
reject_pattern src/slskd/QuarantineJury/QuarantineJuryService.cs 'File\.WriteAllText|File\.Move\(tempPath'
reject_pattern src/slskd/SourceFeeds/SpotifyConnectionService.cs 'File\.WriteAllText|File\.Move\(tempPath'
reject_pattern src/slskd/SourceFeeds/SourceFeedImportService.cs 'File\.WriteAllText|File\.Move\(tempPath'
reject_pattern src/slskd/Integrations/MusicBrainz/Radar/ArtistReleaseRadarService.cs 'File\.WriteAllText|File\.Move\(tempPath'
reject_pattern src/slskd/Integrations/MusicBrainz/Overlay/MusicBrainzOverlayService.cs 'File\.WriteAllText|File\.Move\(tempPath'
reject_pattern src/slskd/Mesh/Realm/SubjectIndex/RealmSubjectIndexService.cs 'File\.WriteAllText|File\.Move\(tempPath'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Durable app state files must use AtomicFileWriter so writes are flushed through
a sibling temp file, atomically replaced, and cleaned up on failure.
MSG
  exit 1
fi

printf 'Durable app state writers use atomic file replacement.\n'
