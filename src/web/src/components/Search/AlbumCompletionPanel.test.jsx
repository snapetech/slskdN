import {
  normalizeAlbumCompletionAlbums,
} from './AlbumCompletionPanel';
import * as musicBrainz from '../../lib/musicBrainz';
import AlbumCompletionPanel from './AlbumCompletionPanel';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/musicBrainz', () => ({ fetchAlbumCompletion: vi.fn() }));

describe('AlbumCompletionPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    musicBrainz.fetchAlbumCompletion.mockResolvedValue({ data: { albums: [] } });
  });

  it('explains that refresh loads current completion data', async () => {
    render(<AlbumCompletionPanel />);

    await screen.findByText(
      'No album targets yet. Resolve a MusicBrainz release or recording to start tracking completion.',
    );

    const refreshButtons = screen.getAllByRole('button', { name: 'Refresh' });
    expect(refreshButtons).toHaveLength(1);
    const [refresh] = refreshButtons;
    expect(refresh).toBeEnabled();
    fireEvent.mouseEnter(refresh);

    expect(await screen.findByText(
      'Fetch the latest album and track completion data from the server.',
    )).toBeInTheDocument();
  });

  it('normalizes malformed album completion payloads before rendering', () => {
    expect(normalizeAlbumCompletionAlbums({
      albums: [
        null,
        'bad',
        ['bad'],
        {
          releaseId: 'release-1',
          title: 'Valid Album',
          tracks: [
            null,
            'bad',
            ['bad'],
            {
              complete: false,
              title: 'Missing Track',
            },
          ],
        },
      ],
    })).toEqual([
      {
        releaseId: 'release-1',
        title: 'Valid Album',
        tracks: [
          {
            complete: false,
            title: 'Missing Track',
          },
        ],
      },
    ]);
  });

  it('returns an empty album list for malformed payload shapes', () => {
    expect(normalizeAlbumCompletionAlbums({ albums: { releaseId: 'bad' } })).toEqual([]);
    expect(normalizeAlbumCompletionAlbums(null)).toEqual([]);
  });
});
