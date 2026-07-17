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
}));

vi.mock('../../lib/spotifyIntegration', () => ({
  disconnectSpotify: vi.fn(),
  getSpotifyStatus: vi.fn(),
  startSpotifyAuthorization: vi.fn(),
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
});
