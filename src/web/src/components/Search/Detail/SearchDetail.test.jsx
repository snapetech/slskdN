import SearchDetail, {
  getInitialResultFilters,
  mapUserNotesByUsername,
  shouldFetchSearchResponses,
} from './SearchDetail';
import { getResponses } from '../../../lib/searches';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';

vi.mock('../../../lib/albumCandidatePicker', () => ({
  buildAlbumCandidates: vi.fn(() => []),
  getAlbumCandidateFilter: vi.fn(),
}));
vi.mock('../../../lib/searchCandidateRanking', () => ({
  rankSearchResponses: vi.fn(({ responses }) => responses),
}));
vi.mock('../../../lib/searchResultDeduplication', () => ({
  deduplicateSearchResponses: vi.fn(({ responses }) => ({
    foldedCount: 0,
    responses,
  })),
}));
vi.mock('../../../lib/searches', () => ({
  blockUser: vi.fn(() => []),
  createBatch: vi.fn(),
  filterResponse: vi.fn(({ response }) => response),
  getBlockedUsers: vi.fn(() => []),
  getResponses: vi.fn(),
  getUserDownloadStats: vi.fn(async () => ({})),
  parseFiltersFromString: vi.fn(() => []),
  unblockUser: vi.fn(() => []),
}));
vi.mock('../../../lib/userNotes', () => ({
  getAllNotes: vi.fn(async () => ({ data: [] })),
}));
vi.mock('../../../lib/wishlist', () => ({
  getIgnoredResults: vi.fn(async () => []),
  ignoreResult: vi.fn(),
}));
vi.mock('../DiscoveryGraphModal', () => ({ default: () => null }));
vi.mock('../Response', () => ({
  default: ({ response }) => (
    <div data-testid="search-response">{response.username}</div>
  ),
}));
vi.mock('./SearchDetailHeader', () => ({ default: () => null }));
vi.mock('./SearchFilterModal', () => ({ default: () => null }));

describe('SearchDetail', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('maps valid user notes and ignores malformed note entries', () => {
    expect(mapUserNotesByUsername([
      null,
      'bad',
      ['bad'],
      { note: 'missing username' },
      { note: 'trusted peer', username: 'alice' },
    ])).toEqual({
      alice: {
        note: 'trusted peer',
        username: 'alice',
      },
    });
  });

  it('returns an empty note map for malformed note payloads', () => {
    expect(mapUserNotesByUsername({ username: 'alice' })).toEqual({});
  });

  it('prefers URL filter over the saved default filter', () => {
    expect(getInitialResultFilters({
      getLocationSearch: () => '?filter=flac+OR+mp3',
      getStoredDefault: () => 'mp3',
    })).toBe('flac OR mp3');
  });

  it('uses saved default filter when URL does not provide one', () => {
    expect(getInitialResultFilters({
      getLocationSearch: () => '?other=value',
      getStoredDefault: () => 'lossless',
    })).toBe('lossless');
  });

  it('hydrates response payloads only when complete or explicitly available', () => {
    expect(shouldFetchSearchResponses({
      isComplete: false,
      responsesAvailable: false,
    })).toBe(false);
    expect(shouldFetchSearchResponses({
      isComplete: false,
      responsesAvailable: true,
    })).toBe(true);
    expect(shouldFetchSearchResponses({
      isComplete: true,
      responsesAvailable: false,
    })).toBe(true);
  });

  it('clears hydrated results when a reused detail changes to an active search without responses', async () => {
    getResponses.mockResolvedValue([
      {
        fileCount: 1,
        files: [],
        hasFreeUploadSlot: true,
        lockedFileCount: 0,
        lockedFiles: [],
        username: 'first-search-peer',
      },
    ]);

    const search = {
      fileCount: 1,
      id: 'first-search',
      isComplete: true,
      lockedFileCount: 0,
      responseCount: 1,
      responsesAvailable: true,
      searchText: 'first',
      state: 'Complete',
    };
    const props = {
      creating: false,
      disabled: false,
      onCreate: vi.fn(),
      onRemove: vi.fn(),
      onStop: vi.fn(),
      removing: false,
      search,
      stopping: false,
    };
    const { rerender } = render(<SearchDetail {...props} />);

    expect(await screen.findByTestId('search-response')).toHaveTextContent(
      'first-search-peer',
    );

    rerender(
      <SearchDetail
        {...props}
        search={{
          ...search,
          fileCount: 0,
          id: 'second-search',
          isComplete: false,
          responseCount: 0,
          responsesAvailable: false,
          searchText: 'second',
          state: 'InProgress',
        }}
      />,
    );

    await waitFor(() =>
      expect(screen.queryByTestId('search-response')).not.toBeInTheDocument(),
    );
    expect(getResponses).toHaveBeenCalledTimes(1);
  });
});
