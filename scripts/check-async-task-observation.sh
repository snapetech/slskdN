#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

reject_literal() {
  local file="$1"
  local literal="$2"

  if grep -Fq -- "$literal" "$repo_root/$file"; then
    printf '%s contains unobserved async call: %s\n' "$file" "$literal" >&2
    failed=1
  fi
}

reject_literal src/slskd/Application.cs '_ = Notifications.SendPrivateMessageAsync'
reject_literal src/slskd/Application.cs '_ = Notifications.SendRoomMentionAsync'
reject_literal src/slskd/Application.cs '_ = TransferHubExtensions.EmitTransferActivityAsync'
reject_literal src/slskd/Application.cs '_ = Client.SendUploadSpeedAsync'
reject_literal src/slskd/Application.cs '_ = Relay.Client.StartAsync'
reject_literal src/slskd/Application.cs '_ = RoomService.TryJoinAsync'
reject_literal src/slskd/Application.cs '_ = ApplicationHub.BroadcastOptionsAsync'
reject_literal src/slskd/Application.cs '_ = Relay.Client.SynchronizeAsync'
reject_literal src/slskd/Application.cs '_ = ApplicationHub.BroadcastStateAsync'

reject_literal src/slskd/Signals/SignalBus.cs '_ = handler.StartReceivingAsync'

reject_literal src/slskd/PodCore/PodServices.cs '_ = chatBridge.ForwardPodToSoulseekAsync'
reject_literal src/slskd/PodCore/PodServices.cs '_ = messageRouter.RouteMessageAsync'

reject_literal src/slskd/Shares/API/Controllers/SharesController.cs '_ = Shares.ScanAsync'

reject_literal src/slskd/Transfers/Downloads/DownloadService.cs '_ = PeerMetrics.RecordThroughputSampleAsync'
reject_literal src/slskd/Transfers/Downloads/DownloadService.cs '_ = PeerMetrics.RecordChunkCompletionAsync'
reject_literal src/slskd/Transfers/Downloads/DownloadService.cs '_ = Relay.NotifyFileDownloadCompleteAsync'
reject_literal src/slskd/Transfers/Downloads/DownloadService.cs '_ = FTP.UploadAsync'

reject_literal src/slskd/DhtRendezvous/MeshOverlayServer.cs '_ = HandleConnectionAsync'
reject_literal src/slskd/DhtRendezvous/MeshOverlayServer.cs '_ = HandleMessagesAsync'
reject_literal src/slskd/DhtRendezvous/MeshOverlayConnector.cs '_ = RunOutboundMessageLoopAsync'
reject_literal src/slskd/Mesh/Overlay/QuicOverlayServer.cs '_ = HandleStreamAsync'
reject_literal src/slskd/Mesh/Overlay/QuicDataServer.cs '_ = HandleStreamAsync'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Async side-effect tasks in app events and transfer completion paths must be observed
through a local helper so post-await failures are logged instead of becoming
unobserved task exceptions.
MSG
  exit 1
fi

printf 'Known async side-effect tasks are observed.\n'
