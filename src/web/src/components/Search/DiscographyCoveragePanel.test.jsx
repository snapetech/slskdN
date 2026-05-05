import { normalizeDiscographyCoverage } from './DiscographyCoveragePanel';

describe('DiscographyCoveragePanel', () => {
  it('normalizes malformed release and track arrays before rendering', () => {
    expect(normalizeDiscographyCoverage({
      artistId: 'artist-1',
      releases: [
        null,
        'bad',
        ['bad'],
        {
          releaseId: 'release-1',
          title: 'Valid Release',
          tracks: [
            null,
            'bad',
            ['bad'],
            {
              status: 'Absent',
              title: 'Missing Track',
            },
          ],
        },
      ],
    })).toEqual({
      artistId: 'artist-1',
      releases: [
        {
          releaseId: 'release-1',
          title: 'Valid Release',
          tracks: [
            {
              status: 'Absent',
              title: 'Missing Track',
            },
          ],
        },
      ],
    });
  });

  it('returns null for malformed coverage payloads', () => {
    expect(normalizeDiscographyCoverage(['bad'])).toBeNull();
    expect(normalizeDiscographyCoverage(null)).toBeNull();
  });
});
