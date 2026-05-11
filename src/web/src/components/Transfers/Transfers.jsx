import './Transfers.css';
import * as autoReplaceLibrary from '../../lib/autoReplace';
import * as transfersLibrary from '../../lib/transfers';
import { LoaderSegment, PlaceholderSegment } from '../Shared';
import TransferGroup from './TransferGroup';
import TransfersHeader from './TransfersHeader';
import React, { useEffect, useMemo, useRef, useState } from 'react';
import { toast } from 'react-toastify';

const AUTO_REPLACE_THRESHOLD = 0; // 0% = exact match only (configurable on backend)

const getErrorMessage = (error) =>
  error?.response?.data ?? error?.message ?? `${error}`;

const summarizeBulkFailures = ({ action, failures }) => {
  if (failures.length === 0) {
    return;
  }

  const [firstFailure] = failures;
  toast.error(
    failures.length === 1
      ? `Failed to ${action} ${firstFailure.label}: ${firstFailure.message}`
      : `Failed to ${action} ${failures.length} transfer(s). First error: ${firstFailure.label}: ${firstFailure.message}`,
  );
};

const getTransferKey = ({ file, suffix = '' }) => {
  return `${file.username}:${file.id}${suffix ? `:${suffix}` : ''}`;
};

const OPTIMISTIC_HIDE_MS = 15_000;
const QUEUE_POSITION_REFRESH_MS = 30_000;
const MAX_QUEUE_POSITION_LOOKUPS_PER_FETCH = 5;

const asArray = (value) => (Array.isArray(value) ? value : []);
const isObject = (value) =>
  value && typeof value === 'object' && !Array.isArray(value);

const normalizeTransfers = (users) =>
  asArray(users)
    .filter(isObject)
    .map((user) => ({
      ...user,
      directories: asArray(user.directories)
        .filter(isObject)
        .map((directory) => ({
          ...directory,
          files: asArray(directory.files).filter(isObject),
        }))
        .filter((directory) => directory.files.length > 0),
    }))
    .filter((user) => user.directories.length > 0);

const Transfers = ({ direction, server }) => {
  const testId = direction === 'download' ? 'downloads-root' : 'uploads-root';
  const [connecting, setConnecting] = useState(true);
  const [transfers, setTransfers] = useState([]);
  const [allTransfers, setAllTransfers] = useState([]);

  const [retryingSingle, setRetryingSingle] = useState(false);
  const [cancellingSingle, setCancellingSingle] = useState(false);
  const [removingSingle, setRemovingSingle] = useState(false);
  const [bulkCounts, setBulkCounts] = useState({ retry: 0, cancel: 0, remove: 0 });

  const [autoReplaceEnabled, setAutoReplaceEnabled] = useState(false);
  const [acceleratedEnabled, setAcceleratedEnabled] = useState(false);
  const [hideCompleted, setHideCompleted] = useState(true);
  const autoReplaceThreshold = AUTO_REPLACE_THRESHOLD;

  const bulkQueueRef = useRef([]);
  const queuedBulkKeysRef = useRef(new Set());
  const bulkQueueRunningRef = useRef(false);
  const hiddenTransfersRef = useRef(new Map());
  const latestFetchIdRef = useRef(0);
  const latestFetchAllIdRef = useRef(0);
  const hideCompletedRef = useRef(true);
  const lastQueuePositionBatchAtRef = useRef(0);
  const queuePositionCacheRef = useRef(new Map());
  const queuePositionRequestsRef = useRef(new Set());

  const retrying = retryingSingle || bulkCounts.retry > 0;
  const cancelling = cancellingSingle || bulkCounts.cancel > 0;
  const removing = removingSingle || bulkCounts.remove > 0;

  const changeBulkCount = (action, delta) => {
    setBulkCounts((previousCounts) => ({
      ...previousCounts,
      [action]: Math.max(0, previousCounts[action] + delta),
    }));
  };

  const isOptimisticallyHidden = (file, now = Date.now()) => {
    const entry = hiddenTransfersRef.current.get(getTransferKey({ file }));

    if (!entry) {
      return false;
    }

    if (entry.until <= now) {
      hiddenTransfersRef.current.delete(getTransferKey({ file }));
      return false;
    }

    return entry.matches(file);
  };

  const filterHiddenTransfers = (users) => {
    const now = Date.now();

    return normalizeTransfers(users)
      .map((user) => ({
        ...user,
        directories: asArray(user.directories)
          .map((directory) => ({
            ...directory,
            files: asArray(directory.files).filter(
              (file) => !isOptimisticallyHidden(file, now),
            ),
          }))
          .filter((directory) => directory.files.length > 0),
      }))
      .filter((user) => user.directories.length > 0);
  };

  const hideTransfers = (files, matches = () => true) => {
    const until = Date.now() + OPTIMISTIC_HIDE_MS;

    files.forEach((file) => {
      hiddenTransfersRef.current.set(getTransferKey({ file }), {
        matches,
        until,
      });
    });

    setTransfers((previousTransfers) =>
      filterHiddenTransfers(previousTransfers),
    );
  };

  const runBulkQueue = async () => {
    if (bulkQueueRunningRef.current) {
      return;
    }

    bulkQueueRunningRef.current = true;

    while (bulkQueueRef.current.length > 0) {
      const queuedOperation = bulkQueueRef.current.shift();

      try {
        await queuedOperation.run();
      } catch (error) {
        queuedOperation.batch.failures.push({
          label: queuedOperation.label,
          message: getErrorMessage(error),
        });
      } finally {
        queuedBulkKeysRef.current.delete(queuedOperation.key);
        changeBulkCount(queuedOperation.action, -1);
        queuedOperation.batch.remaining -= 1;

        if (queuedOperation.batch.remaining === 0) {
          summarizeBulkFailures({
            action: queuedOperation.batch.action,
            failures: queuedOperation.batch.failures,
          });
        }
      }
    }

    bulkQueueRunningRef.current = false;
  };

  const enqueueBulkOperations = ({ action, operations }) => {
    const batch = {
      action,
      failures: [],
      remaining: 0,
    };

    let enqueuedCount = 0;

    operations.forEach((operation) => {
      if (queuedBulkKeysRef.current.has(operation.key)) {
        return;
      }

      queuedBulkKeysRef.current.add(operation.key);
      bulkQueueRef.current.push({
        ...operation,
        action,
        batch,
      });
      batch.remaining += 1;
      enqueuedCount += 1;
    });

    if (enqueuedCount === 0) {
      return;
    }

    changeBulkCount(action, enqueuedCount);
    runBulkQueue();
  };

  const refreshQueuePositions = async (users) => {
    if (direction !== 'download') {
      return;
    }

    const now = Date.now();
    const queuedDownloads = normalizeTransfers(users)
      .flatMap((user) => user.directories.flatMap((dir) => dir.files))
      .filter((file) => file.state && file.state.includes('Queued'));

    queuedDownloads.forEach((file) => {
      const cached = queuePositionCacheRef.current.get(getTransferKey({ file }));
      if (cached?.placeInQueue) {
        file.placeInQueue = cached.placeInQueue;
      }
    });

    if (
      lastQueuePositionBatchAtRef.current > 0 &&
      now - lastQueuePositionBatchAtRef.current < QUEUE_POSITION_REFRESH_MS
    ) {
      return;
    }

    const dueDownloads = queuedDownloads
      .filter((file) => {
        const key = getTransferKey({ file });
        const cached = queuePositionCacheRef.current.get(key);

        return (
          !queuePositionRequestsRef.current.has(key) &&
          (!cached || now - cached.updatedAt >= QUEUE_POSITION_REFRESH_MS)
        );
      })
      .slice(0, MAX_QUEUE_POSITION_LOOKUPS_PER_FETCH);

    if (dueDownloads.length === 0) {
      return;
    }

    lastQueuePositionBatchAtRef.current = now;

    const queuePositionPromises = dueDownloads.map(async (file) => {
      const key = getTransferKey({ file });
      queuePositionRequestsRef.current.add(key);

      try {
        const queueResponse = await transfersLibrary.getPlaceInQueue({
          id: file.id,
          username: file.username,
        });

        queuePositionCacheRef.current.set(key, {
          placeInQueue: queueResponse.data,
          updatedAt: Date.now(),
        });
        file.placeInQueue = queueResponse.data;
      } catch (error) {
        console.debug(
          'Failed to fetch queue position for',
          file.filename,
          error,
        );
      } finally {
        queuePositionRequestsRef.current.delete(key);
      }
    });

    await Promise.allSettled(queuePositionPromises);
  };

  const fetch = async () => {
    const fetchId = ++latestFetchIdRef.current;

    try {
      // Only fetch active (non-completed) transfers for fast polling.
      // allTransfers is populated separately on a slower interval for header bulk ops.
      const response = await transfersLibrary.getAll({ direction, includeCompleted: false });
      const normalizedResponse = normalizeTransfers(response);
      await refreshQueuePositions(normalizedResponse);

      if (fetchId === latestFetchIdRef.current) {
        setTransfers(filterHiddenTransfers(normalizedResponse));
      }
    } catch (error) {
      console.error(error);
      toast.error(getErrorMessage(error));
    }
  };

  const fetchAll = async () => {
    const fetchId = ++latestFetchAllIdRef.current;

    try {
      const response = await transfersLibrary.getAll({ direction });
      const normalized = normalizeTransfers(response);

      if (fetchId === latestFetchAllIdRef.current) {
        setAllTransfers(normalized);
      }
    } catch (error) {
      console.error(error);
    }
  };

  // Keep ref in sync so the stale interval closure reads the current value.
  useEffect(() => {
    hideCompletedRef.current = hideCompleted;
  }, [hideCompleted]);

  useEffect(() => {
    setConnecting(true);

    const init = async () => {
      // Fast first paint: only active transfers.
      await fetch();
      setConnecting(false);
      // Background: populate allTransfers for header bulk ops without blocking.
      fetchAll();
    };

    init();
    const activeInterval = window.setInterval(fetch, 2_000);
    // Slower full-list refresh for header bulk-operation file lists.
    const allInterval = window.setInterval(fetchAll, 15_000);

    return () => {
      clearInterval(activeInterval);
      clearInterval(allInterval);
    };
  }, [direction]); // eslint-disable-line react-hooks/exhaustive-deps

  useMemo(() => {
    setConnecting(true);
  }, [direction]); // eslint-disable-line react-hooks/exhaustive-deps

  const retry = async ({
    file,
    suppressErrorToast = false,
    suppressStateChange = false,
  }) => {
    const { filename, size, username } = file;

    try {
      if (!suppressStateChange) {
        setRetryingSingle(true);
      }

      await transfersLibrary.download({
        files: [{ filename, size }],
        username,
      });
    } catch (error) {
      console.error(error);
      if (!suppressErrorToast) {
        toast.error(getErrorMessage(error));
      }

      throw error;
    } finally {
      if (!suppressStateChange) {
        setRetryingSingle(false);
      }
    }
  };

  const retryAll = (transfersToRetry) => {
    enqueueBulkOperations({
      action: 'retry',
      operations: transfersToRetry.map((file) => ({
        key: `retry:${getTransferKey({ file })}`,
        label: `${file.username}/${file.filename}`,
        run: async () => {
          await retry({
            file,
            suppressErrorToast: true,
            suppressStateChange: true,
          });
          hideTransfers([file], (candidate) =>
            transfersLibrary.isStateRetryable(candidate.state),
          );
        },
      })),
    });
  };

  const cancel = async ({
    file,
    suppressErrorToast = false,
    suppressStateChange = false,
  }) => {
    const { id, username } = file;

    try {
      if (!suppressStateChange) {
        setCancellingSingle(true);
      }

      await transfersLibrary.cancel({ direction, id, username });
    } catch (error) {
      console.error(error);
      if (!suppressErrorToast) {
        toast.error(getErrorMessage(error));
      }

      throw error;
    } finally {
      if (!suppressStateChange) {
        setCancellingSingle(false);
      }
    }
  };

  const cancelAll = (transfersToCancel) => {
    enqueueBulkOperations({
      action: 'cancel',
      operations: transfersToCancel.map((file) => ({
        key: `cancel:${getTransferKey({ file })}`,
        label: `${file.username}/${file.filename}`,
        run: () =>
          cancel({
            file,
            suppressErrorToast: true,
            suppressStateChange: true,
          }),
      })),
    });
  };

  const remove = async ({
    deleteFile = false,
    file,
    suppressErrorToast = false,
    suppressStateChange = false,
  }) => {
    const { id, username } = file;

    try {
      if (!suppressStateChange) {
        setRemovingSingle(true);
      }

      await transfersLibrary.cancel({
        deleteFile,
        direction,
        id,
        remove: true,
        username,
      });
    } catch (error) {
      console.error(error);
      if (!suppressErrorToast) {
        toast.error(getErrorMessage(error));
      }

      throw error;
    } finally {
      if (!suppressStateChange) {
        setRemovingSingle(false);
      }
    }
  };

  const removeAll = (
    transfersToRemove,
    deleteFile = false,
    { useBulkClear = false } = {},
  ) => {
    if (useBulkClear && !deleteFile) {
      enqueueBulkOperations({
        action: 'remove',
        operations: [
          {
            key: `remove:clear-completed:${direction}`,
            label: `all completed ${direction}s`,
            run: async () => {
              await transfersLibrary.clearCompleted({ direction });
              hideTransfers(transfersToRemove);
            },
          },
        ],
      });
      return;
    }

    enqueueBulkOperations({
      action: 'remove',
      operations: transfersToRemove.map((file) => ({
        key: `remove:${getTransferKey({ file, suffix: deleteFile ? 'delete' : 'keep' })}`,
        label: `${file.username}/${file.filename}`,
        run: async () => {
          await remove({
            deleteFile,
            file,
            suppressErrorToast: true,
            suppressStateChange: true,
          });
          hideTransfers([file]);
        },
      })),
    });
  };

  useEffect(() => {
    const fetchDownloadModeStatus = async () => {
      if (direction !== 'download') {
        return;
      }

      try {
        const [autoReplaceStatus, acceleratedStatus] = await Promise.all([
          autoReplaceLibrary.getAutoReplaceStatus(),
          transfersLibrary.getAcceleratedMode(),
        ]);
        setAutoReplaceEnabled(autoReplaceStatus?.enabled ?? false);
        setAcceleratedEnabled(acceleratedStatus?.enabled ?? false);
      } catch (error) {
        console.error('Failed to fetch download mode status:', error);
      }
    };

    fetchDownloadModeStatus();
  }, [direction]);

  const handleAutoReplaceChange = async (enabled) => {
    try {
      if (enabled) {
        await autoReplaceLibrary.enableAutoReplace();
        setAutoReplaceEnabled(true);
        toast.info(
          'Auto-replace enabled. Backend will check for stuck downloads periodically.',
        );
      } else {
        await autoReplaceLibrary.disableAutoReplace();
        setAutoReplaceEnabled(false);
        toast.info('Auto-replace disabled');
      }
    } catch (error) {
      console.error('Failed to toggle auto-replace:', error);
      toast.error('Failed to toggle auto-replace');
    }
  };

  const handleAcceleratedChange = async (enabled) => {
    try {
      const status = await transfersLibrary.setAcceleratedMode({ enabled });
      setAcceleratedEnabled(status?.enabled ?? enabled);
      toast.info(
        enabled
          ? 'Accelerated mode enabled. Slow or stalled downloads can use verified alternate sources.'
          : 'Accelerated mode disabled',
      );
    } catch (error) {
      console.error('Failed to toggle accelerated mode:', error);
      toast.error('Failed to toggle accelerated mode');
    }
  };

  // When hiding completed, `transfers` already contains only active transfers (API-filtered).
  // When showing completed, merge: active poll provides fresh progress, allTransfers provides
  // completed records. Build a fast lookup so active state wins for in-flight transfers.
  const visibleTransfers = useMemo(() => {
    if (hideCompleted) return transfers;

    const activeById = new Map();
    transfers.forEach((user) =>
      user.directories?.forEach((dir) =>
        dir.files?.forEach((file) => activeById.set(file.id, file)),
      ),
    );

    return allTransfers.map((user) => ({
      ...user,
      directories: user.directories?.map((dir) => ({
        ...dir,
        files: dir.files?.map((file) => activeById.get(file.id) ?? file),
      })),
    }));
  }, [transfers, allTransfers, hideCompleted]);

  if (connecting) {
    return <LoaderSegment />;
  }

  return (
    <div data-testid={testId}>
      <TransfersHeader
        acceleratedEnabled={acceleratedEnabled}
        autoReplaceEnabled={autoReplaceEnabled}
        autoReplaceThreshold={autoReplaceThreshold}
        cancelling={cancelling}
        direction={direction}
        hideCompleted={hideCompleted}
        onAutoReplaceChange={handleAutoReplaceChange}
        onAcceleratedChange={handleAcceleratedChange}
        onCancelAll={cancelAll}
        onHideCompletedChange={setHideCompleted}
        onRemoveAll={removeAll}
        onRetryAll={retryAll}
        removing={removing}
        retrying={retrying}
        server={server}
        transfers={allTransfers}
      />
      {allTransfers.length === 0 && transfers.length === 0 ? (
        <PlaceholderSegment
          caption={`No ${direction}s to display`}
          icon={direction}
        />
      ) : hideCompleted && transfers.length === 0 ? (
        <PlaceholderSegment
          caption={`All ${direction}s are completed`}
          icon="check"
        />
      ) : (
        visibleTransfers.map((user) => (
          <TransferGroup
            cancel={cancel}
            cancelAll={cancelAll}
            direction={direction}
            key={user.username}
            remove={remove}
            removeAll={removeAll}
            retry={retry}
            retryAll={retryAll}
            user={user}
          />
        ))
      )}
    </div>
  );
};

export default Transfers;
