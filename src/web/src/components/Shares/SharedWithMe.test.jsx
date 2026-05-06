import SharedWithMe from './SharedWithMe';
import * as collectionsAPI from '../../lib/collections';
import * as identityAPI from '../../lib/identity';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('../../lib/collections', () => ({
  backfillShare: vi.fn(),
  getCollection: vi.fn(),
  getShareManifest: vi.fn(),
  getShares: vi.fn(),
}));

vi.mock('../../lib/identity', () => ({
  getContacts: vi.fn(),
}));

describe('SharedWithMe', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    collectionsAPI.getShares.mockResolvedValue({
      data: [
        {
          allowDownload: true,
          allowStream: true,
          collectionId: 'collection-1',
          id: 'share-1',
        },
      ],
    });
    collectionsAPI.getCollection.mockResolvedValue({
      data: {
        ownerUserId: 'owner-1',
        title: 'Shared Album',
        type: 'ShareList',
      },
    });
    collectionsAPI.getShareManifest.mockResolvedValue({
      data: {
        items: { contentId: 'bad-shape' },
        title: 'Shared Album',
      },
    });
    identityAPI.getContacts.mockResolvedValue({ data: [] });
  });

  it('renders a manifest with malformed items as an empty collection', async () => {
    render(<SharedWithMe />);

    expect(await screen.findByText('Shared Album')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('incoming-share-open'));

    await waitFor(() => expect(collectionsAPI.getShareManifest).toHaveBeenCalledWith('share-1'));
    expect(await screen.findByText('No items in this collection')).toBeInTheDocument();
  });

  it('normalizes malformed backfill responses before rendering results', async () => {
    collectionsAPI.backfillShare.mockResolvedValue({ data: null });

    render(<SharedWithMe />);

    expect(await screen.findByText('Shared Album')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('incoming-share-open'));
    await screen.findByText('No items in this collection');
    fireEvent.click(screen.getByTestId('incoming-backfill'));

    await waitFor(() => expect(collectionsAPI.backfillShare).toHaveBeenCalledWith('share-1'));
    expect(await screen.findByText(/0 enqueued, 0 failed/)).toBeInTheDocument();
  });

  it('renders structured backfill errors as text', async () => {
    collectionsAPI.backfillShare.mockRejectedValue({
      response: {
        data: {
          detail: 'Backfill is disabled for this share',
          status: 400,
          title: 'Bad Request',
        },
      },
    });

    render(<SharedWithMe />);

    expect(await screen.findByText('Shared Album')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('incoming-share-open'));
    await screen.findByText('No items in this collection');
    fireEvent.click(screen.getByTestId('incoming-backfill'));

    expect(await screen.findByText(/Backfill is disabled for this share/))
      .toBeInTheDocument();
  });

  it('renders structured manifest load errors as text', async () => {
    collectionsAPI.getShareManifest.mockRejectedValue({
      response: {
        data: {
          detail: 'Manifest token expired',
          status: 403,
          title: 'Forbidden',
        },
      },
    });

    render(<SharedWithMe />);

    expect(await screen.findByText('Shared Album')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('incoming-share-open'));

    expect(await screen.findByText(/Manifest token expired/)).toBeInTheDocument();
  });
});
