/**
 * Realtime transfer store.
 *
 * Holds a Map keyed by a stable composite key (direction|username|filename)
 * rather than the persisted Guid id. The composite key survives an auto-retry
 * that mints a new record id, so a retried transfer patches its existing row
 * in place instead of churning the list — this is the core fix for the
 * "redraws every time we auto-retry" problem.
 *
 * Seeded from the flat REST snapshot, patched by SignalR ACTIVITY / PROGRESS /
 * REMOVED events, and reconciled against REST every ~15s as a safety net for
 * missed events or a dropped socket.
 */

const norm = (value) => String(value ?? '').toLowerCase();

/**
 * Stable identity for a transfer across retries.
 * @param {{ direction?: string, username?: string, filename?: string }} t
 * @returns {string}
 */
export const transferKey = (t) =>
  `${norm(t?.direction)}|${t?.username ?? ''}|${t?.filename ?? ''}`;

const PATCH_FIELDS = [
  'state',
  'size',
  'bytesTransferred',
  'averageSpeed',
  'percentComplete',
];

export const createTransferStore = () => {
  /** @type {Map<string, object>} */
  const entries = new Map();
  const listeners = new Set();
  let version = 0;
  let cachedList = [];
  let dirty = true;

  const bump = () => {
    version += 1;
    dirty = true;
    listeners.forEach((l) => l());
  };

  const subscribe = (listener) => {
    listeners.add(listener);
    return () => listeners.delete(listener);
  };

  const getVersion = () => version;

  const getAll = () => {
    if (dirty) {
      cachedList = Array.from(entries.values());
      dirty = false;
    }

    return cachedList;
  };

  /**
   * Replace the entire store from a flat REST snapshot. The snapshot is the
   * source of truth on reconcile; recent hub deltas re-apply within ~1s.
   */
  const seed = (records) => {
    entries.clear();

    for (const record of Array.isArray(records) ? records : []) {
      if (record && record.filename) {
        entries.set(transferKey(record), record);
      }
    }

    bump();
  };

  const patch = (event, { allowCreate }) => {
    if (!event || !event.filename) {
      return;
    }

    const key = transferKey(event);
    const existing = entries.get(key);

    if (!existing) {
      if (!allowCreate) {
        return;
      }

      entries.set(key, { ...event });
      bump();
      return;
    }

    const next = { ...existing };
    let changed = false;

    for (const field of PATCH_FIELDS) {
      if (event[field] !== undefined && event[field] !== next[field]) {
        next[field] = event[field];
        changed = true;
      }
    }

    // ACTIVITY carries a freshly resolved id (a retry may have changed it) and
    // an updated queue position; keep them current for API calls / display.
    if (event.id && event.id !== next.id) {
      next.id = event.id;
      changed = true;
    }

    if (
      event.placeInQueue !== undefined &&
      event.placeInQueue !== next.placeInQueue
    ) {
      next.placeInQueue = event.placeInQueue;
      changed = true;
    }

    if (changed) {
      entries.set(key, next);
      bump();
    }
  };

  // State changes can introduce a row we have not seen yet (e.g. a brand new
  // download). Progress events only ever update an existing in-flight row.
  const applyActivity = (activity) => patch(activity, { allowCreate: true });
  const applyProgress = (progress) => patch(progress, { allowCreate: false });

  const removeByKey = (key) => {
    if (entries.delete(key)) {
      bump();
    }
  };

  const applyRemoved = (removed) => {
    if (removed && removed.filename) {
      removeByKey(transferKey(removed));
    }
  };

  return {
    subscribe,
    getVersion,
    getAll,
    seed,
    applyActivity,
    applyProgress,
    applyRemoved,
    removeByKey,
  };
};
