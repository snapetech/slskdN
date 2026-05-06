import Collections from './Collections';
import * as collectionsAPI from '../../lib/collections';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('../../lib/collections', () => ({
  createCollection: vi.fn(),
  createShare: vi.fn(),
  deleteCollection: vi.fn(),
  getCollectionItems: vi.fn(),
  getCollections: vi.fn(),
  getShareGroups: vi.fn(),
  getSharesByCollection: vi.fn(),
  searchLibraryItems: vi.fn(),
}));

vi.mock('../Player/PlayCollectionItemButton', () => ({
  default: () => null,
}));

describe('Collections', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    collectionsAPI.getCollections.mockResolvedValue({
      data: [
        {
          id: 'collection-1',
          itemCount: 0,
          title: 'Fixture Collection',
          type: 'Playlist',
        },
      ],
    });
    collectionsAPI.getShareGroups.mockResolvedValue({
      data: [{ id: 'group-1', name: 'Friends' }],
    });
    collectionsAPI.getCollectionItems.mockResolvedValue({ data: [] });
    collectionsAPI.getSharesByCollection.mockResolvedValue({ data: [] });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders structured delete errors as text', async () => {
    collectionsAPI.deleteCollection.mockRejectedValue({
      response: {
        data: {
          detail: 'Collection is still shared',
          status: 400,
          title: 'Bad Request',
        },
      },
    });

    render(<Collections />);

    await screen.findByText('Fixture Collection');
    fireEvent.click(screen.getByText('Delete'));

    expect(await screen.findByText(/Collection is still shared/))
      .toBeInTheDocument();
  });

  it('renders structured share creation errors as text', async () => {
    collectionsAPI.createShare.mockRejectedValue({
      response: {
        data: {
          detail: 'Share group no longer exists',
          status: 400,
          title: 'Bad Request',
        },
      },
    });

    render(<Collections />);

    fireEvent.click(await screen.findByText('Fixture Collection'));
    await waitFor(() => expect(collectionsAPI.getCollectionItems).toHaveBeenCalled());
    fireEvent.click(screen.getByTestId('share-create'));
    fireEvent.click(await screen.findByTestId('share-create-submit'));

    expect(await screen.findByText(/Share group no longer exists/))
      .toBeInTheDocument();
  });
});
