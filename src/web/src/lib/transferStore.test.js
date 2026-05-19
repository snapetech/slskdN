import { createTransferStore, transferKey } from './transferStore';
import { describe, expect, it } from 'vitest';

const baseTransfer = {
  averageSpeed: 0,
  bytesTransferred: 0,
  direction: 'Download',
  filename: 'Music\\song.flac',
  id: 'id-1',
  percentComplete: 0,
  size: 100,
  state: 'Queued',
  username: 'alice',
};

describe('transferKey', () => {
  it('is stable regardless of direction casing', () => {
    expect(transferKey({ ...baseTransfer, direction: 'Download' })).toBe(
      transferKey({ ...baseTransfer, direction: 'download' }),
    );
  });
});

describe('transferStore', () => {
  it('seeds from a flat snapshot and lists entries', () => {
    const store = createTransferStore();
    store.seed([baseTransfer]);

    expect(store.getAll()).toHaveLength(1);
    expect(store.getAll()[0].username).toBe('alice');
  });

  it('patches an existing row in place on activity (same composite key)', () => {
    const store = createTransferStore();
    store.seed([baseTransfer]);
    const before = store.getVersion();

    store.applyActivity({
      ...baseTransfer,
      bytesTransferred: 50,
      percentComplete: 50,
      state: 'InProgress',
    });

    expect(store.getAll()).toHaveLength(1);
    expect(store.getAll()[0].state).toBe('InProgress');
    expect(store.getAll()[0].percentComplete).toBe(50);
    expect(store.getVersion()).toBeGreaterThan(before);
  });

  it('keeps a single row across an auto-retry that changes the record id', () => {
    const store = createTransferStore();
    store.seed([{ ...baseTransfer, state: 'Completed, Errored' }]);

    // Auto-retry: same peer + filename, brand new persisted id, back to queued.
    store.applyActivity({
      ...baseTransfer,
      id: 'id-2-after-retry',
      state: 'Queued',
    });

    const rows = store.getAll();
    expect(rows).toHaveLength(1);
    expect(rows[0].id).toBe('id-2-after-retry');
    expect(rows[0].state).toBe('Queued');
  });

  it('does not create rows from progress-only events', () => {
    const store = createTransferStore();
    store.applyProgress({ ...baseTransfer, bytesTransferred: 10 });

    expect(store.getAll()).toHaveLength(0);
  });

  it('drops a row on a removed event', () => {
    const store = createTransferStore();
    store.seed([baseTransfer]);

    store.applyRemoved({
      direction: 'Download',
      filename: baseTransfer.filename,
      username: baseTransfer.username,
    });

    expect(store.getAll()).toHaveLength(0);
  });

  it('only notifies subscribers when data actually changes', () => {
    const store = createTransferStore();
    store.seed([baseTransfer]);

    let notifications = 0;
    const unsubscribe = store.subscribe(() => {
      notifications += 1;
    });

    // Identical activity -> no field change -> no notification.
    store.applyActivity({ ...baseTransfer });
    expect(notifications).toBe(0);

    store.applyActivity({ ...baseTransfer, state: 'InProgress' });
    expect(notifications).toBe(1);

    unsubscribe();
  });
});
