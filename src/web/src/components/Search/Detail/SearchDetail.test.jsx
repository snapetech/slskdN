import SearchDetail, {
  batchUsernames,
  getInitialResultFilters,
  mapUserNotesByUsername,
  shouldFetchSearchResponses,
} from './SearchDetail';
import { buildAlbumCandidates } from '../../../lib/albumCandidatePicker';
import { getResponses } from '../../../lib/searches';
import { getGroups } from '../../../lib/users';
import userEvent from '@testing-library/user-event';
import { act, render, screen, waitFor, within } from '@testing-library/react';
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
  createBatch: vi.fn(),
  filterResponse: vi.fn(({ response }) => response),
  getBlockedUsers: vi.fn(() => []),
  getResponses: vi.fn(),
  getUserDownloadStats: vi.fn(async () => ({})),
  parseFiltersFromString: vi.fn(() => []),
}));
vi.mock('../../../lib/userBlocks', () => ({
  blockUserOnServer: vi.fn(async () => []),
  syncBlockedUsers: vi.fn(async () => []),
  unblockUserOnServer: vi.fn(async () => []),
}));
vi.mock('../../../lib/userNotes', () => ({
  getAllNotes: vi.fn(async () => ({ data: [] })),
}));
vi.mock('../../../lib/users', () => ({
  getGroups: vi.fn(async () => ({})),
}));
vi.mock('../../../lib/wishlist', () => ({
  getIgnoredResults: vi.fn(async () => []),
  ignoreResult: vi.fn(),
}));
vi.mock('../DiscoveryGraphModal', () => ({ default: () => null }));
vi.mock('../Response', () => ({
  default: ({ response, userGroup, userGroupLoading }) => (
    <div
      data-group={userGroup || ''}
      data-group-loading={String(userGroupLoading)}
      data-testid="search-response"
    >
      {response.username}
    </div>
  ),
}));
vi.mock('./SearchDetailHeader', () => ({ default: () => null }));
vi.mock('./SearchFilterModal', () => ({ default: () => null }));

describe('SearchDetail', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    buildAlbumCandidates.mockReturnValue([]);
    localStorage.clear();
  });

  afterEach(() => {
    vi.useRealTimers();
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

  it('partitions visible usernames to the bounded group endpoint size', () => {
    const usernames = Array.from({ length: 205 }, (_, index) => `peer-${index}`);

    expect(batchUsernames(usernames)).toEqual([
      usernames.slice(0, 100),
      usernames.slice(100, 200),
      usernames.slice(200),
    ]);
  });

  it('explains that folder ignores require a wishlist search', async () => {
    getResponses.mockResolvedValue([]);

    render(<SearchDetail {...createProps()} />);

    expect(await screen.findByRole('note')).toHaveTextContent(
      'Folder ignores are available only for wishlist searches',
    );
    await waitFor(() => expect(getResponses).toHaveBeenCalledTimes(1));
  });

  it('explains where wishlist folder ignores apply', async () => {
    getResponses.mockResolvedValue([]);

    render(
      <SearchDetail
        {...createProps({ wishlistItemId: 'wishlist-item' })}
      />,
    );

    expect(await screen.findByRole('note')).toHaveTextContent(
      'hide that peer and folder from future runs of this wishlist item',
    );
    await waitFor(() => expect(getResponses).toHaveBeenCalledTimes(1));
  });

  it('saves a named result filter through an in-app dialog', async () => {
    const user = userEvent.setup();
    getResponses.mockResolvedValue([]);
    render(<SearchDetail {...createProps()} />);
    await waitFor(() => expect(getResponses).toHaveBeenCalledTimes(1));

    const filterInput = screen.getByPlaceholderText(/lackluster/);
    await user.type(filterInput, 'minfilesize:10mb');
    await user.click(await screen.findByTitle('Save named filter'));

    const dialog = await screen.findByTestId('save-search-filter-dialog');
    const nameInput = within(dialog).getByRole('textbox', { name: 'Filter name' });
    expect(nameInput).toHaveValue('test');
    await user.clear(nameInput);
    await user.type(nameInput, 'Large files');
    await user.click(within(dialog).getByRole('button', { name: 'Save Filter' }));

    expect(JSON.parse(localStorage.getItem('slskd-saved-search-filters')))
      .toEqual([{ name: 'Large files', value: 'minfilesize:10mb' }]);
    expect(screen.queryByTestId('save-search-filter-dialog')).not.toBeInTheDocument();
  });

  it('does not start completed-result hydration after unmount during the delay', async () => {
    vi.useFakeTimers();
    getResponses.mockResolvedValue([]);
    const { unmount } = render(<SearchDetail {...createProps()} />);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });
    unmount();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(500);
    });

    expect(getResponses).not.toHaveBeenCalled();
    vi.useRealTimers();
  });

  it('skips the album-candidate panel when the browser preference is disabled', async () => {
    localStorage.setItem(
      'slskdn:experience-preferences:v1',
      JSON.stringify({ searchAlbumCandidatesVisible: false }),
    );
    buildAlbumCandidates.mockReturnValue([
      {
        albumTitle: 'Hidden Album',
      },
    ]);
    getResponses.mockResolvedValue([]);

    render(<SearchDetail {...createProps()} />);

    await waitFor(() => expect(getResponses).toHaveBeenCalledTimes(1));
    expect(buildAlbumCandidates).not.toHaveBeenCalled();
    expect(screen.queryByText('Album candidates')).not.toBeInTheDocument();
  });

  it('clears hydrated results when a reused detail changes to an active search without responses', async () => {
    getGroups.mockResolvedValue({ 'first-search-peer': 'privileged' });
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

    const response = await screen.findByTestId('search-response');
    expect(response).toHaveTextContent('first-search-peer');
    await waitFor(() =>
      expect(response).toHaveAttribute('data-group', 'privileged'),
    );
    expect(getGroups).toHaveBeenCalledWith({
      usernames: ['first-search-peer'],
    });

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

const createProps = (searchOverrides = {}) => ({
  creating: false,
  disabled: false,
  onCreate: vi.fn(),
  onRemove: vi.fn(),
  onStop: vi.fn(),
  removing: false,
  search: {
    fileCount: 0,
    id: 'search-id',
    isComplete: true,
    lockedFileCount: 0,
    responseCount: 0,
    responsesAvailable: true,
    searchText: 'test',
    state: 'Complete',
    ...searchOverrides,
  },
  stopping: false,
});
