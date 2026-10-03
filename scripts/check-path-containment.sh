#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

expect_literal() {
  local file="$1"
  local literal="$2"

  if ! grep -Fq -- "$literal" "$repo_root/$file"; then
    printf '%s is missing literal: %s\n' "$file" "$literal" >&2
    failed=1
  fi
}

expect_literal src/slskd/Relay/RelayClient.cs 'PathGuard.NormalizeAndValidate'
expect_literal src/slskd/Relay/RelayClient.cs 'CopyWithLimitAsync'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'IOPath.GetFileName'
expect_literal src/slskd/LibraryHealth/LibraryHealthService.cs 'NormalizeAbsolutePathWithinRoots'
expect_literal src/slskd/Files/FileService.cs 'PathGuard.NormalizeAbsolutePathWithinRoots(path, AllowedDirectories)'
expect_literal src/slskd/Files/API/FilesController.cs 'PathGuard.NormalizeAndValidate('
expect_literal src/slskd/API/Native/LibraryItemsController.cs 'PathGuard.NormalizeAbsolutePathWithinRoots(filename, localDirs)'
expect_literal src/slskd/API/Native/LibraryItemsController.cs 'AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint'
expect_literal src/slskd/Streaming/ContentLocator.cs 'NormalizeAllowedLocalPath'
expect_literal src/slskd/Search/API/Controllers/SearchActionsController.cs 'PathGuard.NormalizeAbsolutePathWithinRoots(candidateFilename, new[] { completedRoot })'
expect_literal src/slskd/Search/API/Controllers/SearchActionsController.cs 'SecureFileWriter.Open(stagingFilename, completedRoot)'
expect_literal src/slskd/Search/API/Controllers/SearchActionsController.cs 'ContentSafety.PublishStagedFile(stagingFilename, localFilename, completedRoot, overwrite: true)'
expect_literal src/slskd/Transfers/Downloads/DownloadService.cs 'PathGuard.NormalizeAbsolutePathWithinRoots(localFilename, new[] { incompleteRoot })'
expect_literal src/slskd/Transfers/Downloads/DownloadService.cs 'NormalizeCompletedDestination(destination, options)'
expect_literal src/slskd/Transfers/Downloads/DownloadService.cs 'PathGuard.NormalizeAbsolutePathWithinRoots(localFilename, allowedRoots)'
expect_literal src/slskd/Shares/ShareService.cs 'PathGuard.NormalizeAbsolutePathWithinRoots'
expect_literal src/slskd/Mesh/MeshSyncService.cs 'PathGuard.NormalizeAbsolutePathWithinRoots(indexedPath, allowedRoots)'
expect_literal src/slskd/Mesh/ServiceFabric/Services/MeshContentMeshService.cs '_shareService.ResolveFileAsync(ci.Value.MaskedFilename)'
expect_literal src/slskd/Shares/ShareScanner.cs 'FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint'
expect_literal src/slskd/Sharing/API/SharesController.cs 'PathGuard.NormalizeAndValidate(candidateFilename, downloadsDir)'
expect_literal src/slskd/Sharing/API/SharesController.cs 'SecureFileWriter.Open(stagingPath, downloadsDir)'
expect_literal src/slskd/Sharing/API/SharesController.cs 'ContentSafety.PublishStagedFile(stagingPath, filePath, downloadsDir)'
expect_literal src/slskd/Common/Security/PathGuard.cs 'NormalizeAndValidate'
expect_literal src/slskd/Common/Security/PathGuard.cs 'NormalizeAbsolutePathWithinRoots'
expect_literal src/slskd/Relay/RelayService.cs 'bool TryValidateFileDownloadCredential(Guid token, string credential, out string validatedAgentName, out string validatedFilename)'
expect_literal src/slskd/Relay/API/Controllers/RelayController.cs 'PathGuard.NormalizeAndValidate(validatedFilename'
expect_literal src/slskd/Files/FileService.cs 'enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;'
expect_literal src/slskd/Files/API/FilesController.cs 'AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint'
expect_literal src/slskd/Streaming/ContentLocator.cs 'AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint'
expect_literal src/slskd/LibraryHealth/LibraryHealthService.cs 'AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint'

if grep -Eq 'PathGuard\.NormalizeAndValidate\((filename|requestedFilename)' "$repo_root/src/slskd/Relay/API/Controllers/RelayController.cs"; then
  printf '%s uses a request filename for relay download path selection\n' "src/slskd/Relay/API/Controllers/RelayController.cs" >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Known remote/user-derived file write paths must keep PathGuard containment.
Broader file-operation inventories belong in the bug ledger until false positives are triaged.
MSG
  exit 1
fi

printf 'Known remote/user-derived file paths are paired with containment helpers.\n'
