import {
  normalizeAlbumCompletionAlbums,
} from './AlbumCompletionPanel';

describe('AlbumCompletionPanel', () => {
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
