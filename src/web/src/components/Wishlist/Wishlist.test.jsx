import Wishlist from './Wishlist';
import * as spotifyIntegrationAPI from '../../lib/spotifyIntegration';
import * as wishlistAPI from '../../lib/wishlist';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../lib/wishlist', () => ({
  create: vi.fn(),
  getAll: vi.fn(),
  getSearches: vi.fn(),
  importCsv: vi.fn(),
  markAllViewed: vi.fn(),
  markViewed: vi.fn(),
  remove: vi.fn(),
  runSearch: vi.fn(),
  update: vi.fn(),
  updateFilters: vi.fn(),
}));

vi.mock('../../lib/spotifyIntegration', () => ({
  disconnectSpotify: vi.fn(),
  getSpotifyStatus: vi.fn(),
  startSpotifyAuthorization: vi.fn(),
}));

vi.mock('../../lib/userBlocks', () => ({
  syncBlockedUsers: vi.fn(async () => []),
}));

const renderWishlist = () =>
  render(
    <MemoryRouter>
      <Wishlist />
    </MemoryRouter>,
  );

describe('Wishlist', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    Object.assign(navigator, {
      clipboard: {
        writeText: vi.fn().mockResolvedValue(undefined),
      },
    });
    spotifyIntegrationAPI.getSpotifyStatus.mockResolvedValue({
      configured: false,
      connected: false,
    });
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        filter: 'flac',
        id: 'wish-1',
        lastMatchCount: 0,
        lastSearchedAt: null,
        searchText: 'rare album',
        totalSearchCount: 0,
      },
      {
        autoDownload: true,
        enabled: true,
        id: 'wish-2',
        lastMatchCount: 3,
        lastSearchedAt: '2026-04-30T19:30:00Z',
        searchText: 'auto track',
        totalSearchCount: 2,
      },
    ]);
    wishlistAPI.getSearches.mockResolvedValue([]);
    wishlistAPI.markAllViewed.mockResolvedValue({});
    wishlistAPI.markViewed.mockResolvedValue({});
  });

  it('shows unified request states for wishlist rows', async () => {
    renderWishlist();

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    expect(screen.getByText('Wanted')).toBeInTheDocument();
    expect(screen.getAllByText('Automatic').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Request Portal Summary')).toBeInTheDocument();
    expect(screen.getByText('23 left')).toBeInTheDocument();
  });

  it('gives icon-only view mode buttons accessible names', async () => {
    renderWishlist();

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Show wishlist as a table' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Show wishlist as cards' }),
    ).toBeInTheDocument();
  });

  it('explains wishlist row actions and defers peer searches until clicked', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        filter: 'flac',
        id: 'wish-1',
        lastMatchCount: 0,
        lastSearchedAt: null,
        searchText: 'rare album',
        totalSearchCount: 0,
      },
    ]);
    renderWishlist();

    const expectTooltip = async (button, tooltip) => {
      const trigger = button.disabled ? button.parentElement : button;
      fireEvent.mouseEnter(trigger);
      expect(await screen.findByText(tooltip)).toBeInTheDocument();
      fireEvent.mouseLeave(trigger);
    };

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    await expectTooltip(
      screen.getByRole('button', { name: 'Show rare album search history' }),
      'Review this item’s linked search history and past results.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Run rare album wishlist search now' }),
      'Manually search Soulseek for current matches. This contacts peers now rather than waiting for the next scheduled run.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Edit rare album wishlist settings' }),
      'Change this item’s search text, filters, result limits, and automation settings.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Delete rare album from Wishlist' }),
      'Remove this wishlist entry after confirmation when you no longer want its search tracked.',
    );

    expect(wishlistAPI.getSearches).not.toHaveBeenCalled();
    expect(wishlistAPI.runSearch).not.toHaveBeenCalled();
    expect(wishlistAPI.remove).not.toHaveBeenCalled();
    expect(screen.queryByText('Edit Wishlist Item')).not.toBeInTheDocument();
  });

  it('names linked search history actions for assistive technology', async () => {
    wishlistAPI.getSearches.mockResolvedValue([{
      id: 'search-1',
      responseCount: 4,
      searchText: 'rare album',
      startedAt: '2026-10-05T18:00:00Z',
    }]);
    renderWishlist();

    await screen.findByText('rare album');
    fireEvent.click(screen.getByRole('button', { name: 'Show rare album search history' }));

    expect(await screen.findByRole('button', { name: 'Open the full search for rare album' }))
      .toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Show rare album results inline' }))
      .toBeInTheDocument();
  });

  it('shows one delete confirmation with guidance in table and card views', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        id: 'wish-1',
        lastMatchCount: 0,
        lastSearchedAt: null,
        searchText: 'rare album',
        totalSearchCount: 0,
      },
    ]);
    renderWishlist();

    const expectTooltip = async (button, tooltip) => {
      fireEvent.mouseEnter(button);
      expect(await screen.findByText(tooltip)).toBeInTheDocument();
      fireEvent.mouseLeave(button);
    };

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Delete rare album from Wishlist' }));
    expect(screen.getAllByText('Confirm Delete')).toHaveLength(1);
    await expectTooltip(
      screen.getByRole('button', { name: 'Cancel' }),
      'Keep this wishlist item and return to the list.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Delete' }),
      'Remove this wishlist item and its saved search settings.',
    );
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    fireEvent.click(screen.getByRole('button', { name: 'Show wishlist as cards' }));
    await expectTooltip(
      screen.getByRole('button', { name: 'Run rare album wishlist search now' }),
      'Manually search Soulseek for current matches. This contacts peers now rather than waiting for the next scheduled run.',
    );
    fireEvent.click(screen.getByRole('button', { name: 'Delete rare album from Wishlist' }));
    expect(screen.getAllByText('Confirm Delete')).toHaveLength(1);
    await expectTooltip(
      screen.getByRole('button', { name: 'Cancel' }),
      'Keep this wishlist item and return to the list.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Delete' }),
      'Remove this wishlist item and its saved search settings.',
    );
    expect(wishlistAPI.remove).not.toHaveBeenCalled();
  });

  it('explains add and bulk-filter modal actions without running them on hover', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        filter: 'flac',
        id: 'wish-1',
        lastMatchCount: 0,
        lastSearchedAt: null,
        searchText: 'rare album',
        totalSearchCount: 0,
      },
    ]);
    renderWishlist();

    const expectTooltip = async (button, tooltip) => {
      const trigger = button.disabled ? button.parentElement : button;
      fireEvent.mouseEnter(trigger);
      expect(await screen.findByText(tooltip)).toBeInTheDocument();
      fireEvent.mouseLeave(trigger);
    };

    await screen.findByText('rare album');
    fireEvent.click(screen.getByRole('button', { name: 'Add Search' }));
    await expectTooltip(
      screen.getByRole('button', { name: 'Cancel' }),
      'Close this form and discard unsaved wishlist changes.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Add' }),
      'Add this search to the wishlist with the filter and automation settings below.',
    );
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    fireEvent.click(screen.getByRole('checkbox', { name: 'Select rare album for bulk actions' }));
    await expectTooltip(
      screen.getByRole('button', { name: 'Clear' }),
      'Clear the selection so bulk actions no longer affect these wishlist items.',
    );
    fireEvent.click(screen.getByRole('button', { name: 'Edit filters for selected wishlist items' }));
    await expectTooltip(
      screen.getByRole('button', { name: 'Cancel' }),
      'Close without changing filters on the selected wishlist items.',
    );
    await expectTooltip(
      screen.getByRole('button', { name: 'Apply Filter' }),
      'Apply this filter to 1 selected wishlist item in one step.',
    );

    expect(wishlistAPI.create).not.toHaveBeenCalled();
    expect(wishlistAPI.updateFilters).not.toHaveBeenCalled();
  });

  it('explains the empty wishlist action', async () => {
    wishlistAPI.getAll.mockResolvedValue([]);
    renderWishlist();

    const button = await screen.findByRole('button', { name: 'Add Your First Search' });
    fireEvent.mouseEnter(button);

    expect(await screen.findByText(
      'Add a search to your wishlist so you can track it manually or automate it with the options you choose.',
    )).toBeInTheDocument();
    expect(wishlistAPI.create).not.toHaveBeenCalled();
  });

  it('labels the current wishlist page as status and explains pagination', async () => {
    localStorage.setItem('slskdn-wishlist-view-state', JSON.stringify({ pageSize: 50 }));
    wishlistAPI.getAll.mockResolvedValue(Array.from({ length: 51 }, (_, index) => ({
      autoDownload: false,
      enabled: true,
      id: `wish-${index}`,
      lastMatchCount: 0,
      lastSearchedAt: null,
      searchText: `album ${index}`,
      totalSearchCount: 0,
    })));
    renderWishlist();

    expect(await screen.findByText('album 0')).toBeInTheDocument();
    const pageStatus = screen.getByRole('status', { name: 'Page 1 of 2' });
    expect(pageStatus).toHaveTextContent('1/2');
    fireEvent.mouseEnter(pageStatus);

    expect(await screen.findByText(
      'Shows your current wishlist page. Use the arrow buttons to move between pages.',
    )).toBeInTheDocument();
  });

  it('keeps wishlist rows on direct request states without inbox promotion', async () => {
    renderWishlist();

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    expect(screen.queryByTitle('Send to Discovery Inbox review')).not.toBeInTheDocument();
    expect(screen.getByText('Wanted')).toBeInTheDocument();
  });

  it('copies a wishlist request review packet', async () => {
    renderWishlist();

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Copy Wishlist request review' }));

    await waitFor(() => {
      expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
        expect.stringContaining('slskdN Wishlist request review'),
      );
    });
  });

  it('runs enabled wishlist searches in a bounded batch', async () => {
    wishlistAPI.runSearch.mockResolvedValue({ responseCount: 4 });

    renderWishlist();

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Run enabled Wishlist searches' }));

    await waitFor(() => {
      expect(wishlistAPI.runSearch).toHaveBeenCalledTimes(2);
    });
    expect(wishlistAPI.runSearch).toHaveBeenCalledWith('wish-1');
    expect(wishlistAPI.runSearch).toHaveBeenCalledWith('wish-2');
    expect(screen.getByText(/Ran 2 enabled Wishlist searches/)).toBeInTheDocument();
  });

  it('renders an empty wishlist when the API returns a malformed list', async () => {
    wishlistAPI.getAll.mockResolvedValue({ items: [] });

    renderWishlist();

    expect(await screen.findByText('No wishlist items yet')).toBeInTheDocument();
  });

  it('encodes last search ids as one route segment', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        id: 'wish-special',
        lastMatchCount: 1,
        lastSearchId: 'search/with?chars%',
        lastSearchedAt: '2026-05-06T00:00:00Z',
        searchText: 'encoded route',
        totalSearchCount: 1,
      },
    ]);

    renderWishlist();

    expect(await screen.findByText('encoded route')).toBeInTheDocument();
    expect(screen.getByTitle('View last search results').closest('a')).toHaveAttribute(
      'href',
      '/searches/search%2Fwith%3Fchars%25',
    );
  });

  it('passes wishlist filters when opening latest search results', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        filter: 'flac OR mp3',
        id: 'wish-filtered',
        lastMatchCount: 1,
        lastSearchId: 'search-filtered',
        lastSearchedAt: '2026-05-06T00:00:00Z',
        searchText: 'filtered route',
        totalSearchCount: 1,
      },
    ]);

    renderWishlist();

    expect(await screen.findByText('filtered route')).toBeInTheDocument();
    expect(screen.getByTitle('View last search results').closest('a')).toHaveAttribute(
      'href',
      '/searches/search-filtered?filter=flac+OR+mp3',
    );
  });

  it('marks a single wishlist item as viewed without opening history', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        id: 'wish-new',
        lastMatchCount: 2,
        lastSearchId: 'search-new',
        lastSearchedAt: '2026-05-06T00:00:00Z',
        searchText: 'new results',
        totalSearchCount: 1,
      },
    ]);

    renderWishlist();

    expect(await screen.findByText('new results')).toBeInTheDocument();
    fireEvent.click(screen.getByTitle('Mark viewed'));

    await waitFor(() => {
      expect(wishlistAPI.markViewed).toHaveBeenCalledWith('wish-new');
    });
  });

  it('does not mark new results viewed when opening the latest search', async () => {
    wishlistAPI.getAll.mockResolvedValue([
      {
        autoDownload: false,
        enabled: true,
        id: 'wish-new',
        lastMatchCount: 2,
        lastSearchId: 'search-new',
        lastSearchedAt: '2026-05-06T00:00:00Z',
        lastViewedAt: '2026-05-05T00:00:00Z',
        searchText: 'new results',
        totalSearchCount: 1,
      },
    ]);

    renderWishlist();

    expect(await screen.findByText('new results')).toBeInTheDocument();
    fireEvent.click(screen.getByTitle('View last search results'));

    expect(wishlistAPI.markViewed).not.toHaveBeenCalled();
  });

  it('bulk-edits filters for selected wishlist items', async () => {
    wishlistAPI.updateFilters.mockResolvedValue({ updatedCount: 2 });

    renderWishlist();

    expect(await screen.findByText('rare album')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Select rare album for bulk actions' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Select auto track for bulk actions' }));
    fireEvent.click(screen.getByRole('button', { name: 'Edit filters for selected wishlist items' }));

    expect(await screen.findByText('Edit Filter for 2 Wishlist Items')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'MP3 320+' }));
    fireEvent.click(screen.getByRole('button', { name: 'Apply Filter' }));

    await waitFor(() => {
      expect(wishlistAPI.updateFilters).toHaveBeenCalledTimes(1);
    });
    expect(wishlistAPI.updateFilters).toHaveBeenCalledWith(
      expect.arrayContaining(['wish-1', 'wish-2']),
      'mp3 minbr:320',
    );
  });
});
