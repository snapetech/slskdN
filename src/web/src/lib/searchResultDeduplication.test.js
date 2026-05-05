import { deduplicateSearchResponses } from './searchResultDeduplication';

describe('deduplicateSearchResponses', () => {
  it('folds duplicate media candidates while keeping the first ranked response', () => {
    const result = deduplicateSearchResponses({
      responses: [
        {
          files: [
            {
              filename: 'Artist/Album/01 Track.flac',
              size: 24_000_000,
            },
          ],
          primarySource: 'mesh',
          smartScore: 91,
          sourceProviders: ['mesh'],
          username: 'best-peer',
        },
        {
          files: [
            {
              filename: 'Different Root/01 Track.flac',
              size: 24_000_000,
            },
          ],
          primarySource: 'soulseek',
          smartScore: 75,
          sourceProviders: ['soulseek'],
          username: 'backup-peer',
        },
        {
          files: [
            {
              filename: 'Artist/Album/02 Other.flac',
              size: 22_000_000,
            },
          ],
          smartScore: 70,
          username: 'other-peer',
        },
      ],
    });

    expect(result.responses).toHaveLength(2);
    expect(result.foldedCount).toBe(1);
    expect(result.responses[0].username).toBe('best-peer');
    expect(result.responses[0].foldedCandidates).toHaveLength(1);
    expect(result.responses[0].duplicateGroup).toEqual(
      expect.objectContaining({
        candidateCount: 2,
        foldedCount: 1,
        providers: ['mesh', 'soulseek'],
        usernames: ['backup-peer', 'best-peer'],
      }),
    );
  });

  it('returns the original response list when folding is disabled', () => {
    const responses = [
      {
        files: [{ filename: 'Artist/Album/01 Track.flac', size: 24_000_000 }],
        username: 'peer-a',
      },
      {
        files: [{ filename: 'Other/01 Track.flac', size: 24_000_000 }],
        username: 'peer-b',
      },
    ];

    const result = deduplicateSearchResponses({
      enabled: false,
      responses,
    });

    expect(result.responses).toBe(responses);
    expect(result.foldedCount).toBe(0);
  });

  it('ignores malformed source provider lists while folding duplicates', () => {
    const result = deduplicateSearchResponses({
      responses: [
        {
          files: [{ filename: 'Artist/Album/01 Track.flac', size: 24_000_000 }],
          primarySource: 'mesh',
          sourceProviders: { 0: 'bad', length: 1 },
          username: 'best-peer',
        },
        {
          files: [{ filename: 'Other Root/01 Track.flac', size: 24_000_000 }],
          primarySource: 'soulseek',
          username: 'backup-peer',
        },
      ],
    });

    expect(result.responses[0].duplicateGroup.providers).toEqual([
      'mesh',
      'soulseek',
    ]);
  });
});
