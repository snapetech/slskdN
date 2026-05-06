import ShareGroups from './ShareGroups';
import * as collectionsAPI from '../../lib/collections';
import * as identityAPI from '../../lib/identity';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('../../lib/collections', () => ({
  addShareGroupMember: vi.fn(),
  createShareGroup: vi.fn(),
  deleteShareGroup: vi.fn(),
  getShareGroups: vi.fn(),
  getShareGroupMembers: vi.fn(),
  removeShareGroupMember: vi.fn(),
}));

vi.mock('../../lib/identity', () => ({
  getContacts: vi.fn(),
}));

describe('ShareGroups', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    collectionsAPI.getShareGroups.mockResolvedValue({
      data: [
        {
          createdAt: '2026-05-06T00:00:00Z',
          id: 'group-1',
          name: 'Friends',
        },
      ],
    });
    identityAPI.getContacts.mockResolvedValue({ data: [] });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders structured delete errors as text', async () => {
    collectionsAPI.deleteShareGroup.mockRejectedValue({
      response: {
        data: {
          detail: 'Share group still has grants',
          status: 400,
          title: 'Bad Request',
        },
      },
    });

    render(<ShareGroups />);

    await screen.findByText('Friends');
    fireEvent.click(screen.getByText('Delete'));

    expect(await screen.findByText(/Share group still has grants/))
      .toBeInTheDocument();
  });
});
