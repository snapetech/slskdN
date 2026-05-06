import TransferGroup from './TransferGroup';

describe('TransferGroup', () => {
  const makeGroup = (user) =>
    new TransferGroup({
      cancelAll: vi.fn(),
      direction: 'download',
      removeAll: vi.fn(),
      retryAll: vi.fn(),
      user,
    });

  it('ignores malformed and stale selected transfer entries', () => {
    const group = makeGroup({
      directories: [
        {
          directory: 'kept',
          files: [{ filename: 'track.flac', state: 'Completed' }],
        },
      ],
      username: 'alice',
    });

    group.state.selections = new Set([
      '{bad json',
      JSON.stringify({ directory: 'missing', filename: 'ghost.flac' }),
      JSON.stringify({ directory: 'kept', filename: 'track.flac' }),
    ]);

    expect(group.getSelectedFiles()).toEqual([
      { filename: 'track.flac', state: 'Completed' },
    ]);
  });

  it('treats malformed directory lists as empty while resolving selections', () => {
    const group = makeGroup({
      directories: { directory: 'not-an-array' },
      username: 'alice',
    });

    group.state.selections = new Set([
      JSON.stringify({ directory: 'not-an-array', filename: 'track.flac' }),
    ]);

    expect(group.getSelectedFiles()).toEqual([]);
  });
});
