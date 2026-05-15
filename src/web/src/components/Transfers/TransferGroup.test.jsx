import TransferGroup from './TransferGroup';
import TransferList from './TransferList';
import React from 'react';
import { render, screen } from '@testing-library/react';

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

  it('labels failed terminal downloads without saying completed', () => {
    render(
      <TransferList
        direction="download"
        directoryName="Album"
        files={[
          {
            bytesTransferred: 12,
            direction: 'Download',
            filename: 'Album\\track.flac',
            percentComplete: 12,
            size: 100,
            state: 'Completed, Errored',
          },
        ]}
        onPlaceInQueueRequested={vi.fn()}
        onRetryRequested={vi.fn()}
        onSelectionChange={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: /Error/u })).toBeInTheDocument();
    expect(screen.queryByText(/Completed/u)).not.toBeInTheDocument();
  });

  it('labels expected remote download failures as peer unavailable', () => {
    render(
      <TransferList
        direction="download"
        directoryName="Album"
        files={[
          {
            bytesTransferred: 0,
            direction: 'Download',
            exception: 'Transfer failed: Read error: Remote connection closed',
            filename: 'Album\\track.flac',
            percentComplete: 0,
            size: 100,
            state: 'Completed, Errored',
          },
        ]}
        onPlaceInQueueRequested={vi.fn()}
        onRetryRequested={vi.fn()}
        onSelectionChange={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: /Peer unavailable/u })).toBeInTheDocument();
    expect(screen.queryByText(/^Error$/u)).not.toBeInTheDocument();
  });
});
