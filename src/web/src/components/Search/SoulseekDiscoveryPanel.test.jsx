// <copyright file="SoulseekDiscoveryPanel.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import SoulseekDiscoveryPanel from './SoulseekDiscoveryPanel';
import * as soulseekDiscovery from '../../lib/soulseekDiscovery';
import * as wishlist from '../../lib/wishlist';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/soulseekDiscovery', () => ({
  addHatedInterest: vi.fn(),
  addInterest: vi.fn(),
  getGlobalRecommendations: vi.fn(),
  getItemRecommendations: vi.fn(),
  getItemSimilarUsers: vi.fn(),
  getRecommendations: vi.fn(),
  getSimilarUsers: vi.fn(),
  getUserInterests: vi.fn(),
  removeHatedInterest: vi.fn(),
  removeInterest: vi.fn(),
}));

vi.mock('../../lib/wishlist', () => ({
  create: vi.fn(),
}));

describe('SoulseekDiscoveryPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    soulseekDiscovery.getRecommendations.mockResolvedValue({
      data: {
        recommendations: [{ item: 'Deep Dub', score: 42 }],
        unrecommendations: [],
      },
    });
    soulseekDiscovery.getSimilarUsers.mockResolvedValue({
      data: [{ rating: 9, username: 'taste-peer' }],
    });
    soulseekDiscovery.getUserInterests.mockResolvedValue({
      data: {
        hated: ['noise'],
        liked: ['dub'],
      },
    });
    wishlist.create.mockResolvedValue({});
  });

  it('loads native recommendations and hands them to search and Wishlist', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<SoulseekDiscoveryPanel onSearch={onSearch} />);

    await user.click(screen.getByRole('button', { name: 'My Recs' }));

    expect(await screen.findByText('Deep Dub')).toBeInTheDocument();
    await user.click(screen.getByLabelText('Search Deep Dub'));
    expect(onSearch).toHaveBeenCalledWith('Deep Dub');

    await user.click(screen.getByLabelText('Add Deep Dub to Wishlist'));
    await waitFor(() =>
      expect(wishlist.create).toHaveBeenCalledWith(
        expect.objectContaining({
          enabled: false,
          searchText: 'Deep Dub',
        }),
      ),
    );
  });

  it('explains the manual interest and recommendation actions', async () => {
    render(<SoulseekDiscoveryPanel />);

    const guidance = [
      ['Add Interest', 'Add this text to your Soulseek interest profile to shape future recommendations.'],
      ['Add Hated', 'Mark this text as something you do not want recommended.'],
      ['Remove Interest', 'Remove this text from your interest profile.'],
      ['Remove Hated', 'Stop suppressing this text from recommendations.'],
      ['My Recs', 'Load recommendations based on your own Soulseek interests.'],
      ['Global', 'Load shared recommendations across the Soulseek community.'],
      ['Similar Users', 'Find users with interests similar to yours.'],
      ['Item Recs', 'Find recommendations related to the item you entered.'],
      ['Item Users', 'Find users who are similar to people interested in this item.'],
      ['User Interests', "Fetch the entered user's public interest and hated-item lists."],
    ];

    for (const [label, tooltip] of guidance) {
      const button = screen.getByRole('button', { name: label });
      fireEvent.mouseEnter(button);
      expect(await screen.findByText(tooltip)).toBeInTheDocument();
    }
  });

  it('loads similar users and then user interests on demand', async () => {
    const user = userEvent.setup();
    render(<SoulseekDiscoveryPanel />);

    await user.click(screen.getByRole('button', { name: 'Similar Users' }));

    expect(await screen.findByText('taste-peer')).toBeInTheDocument();
    await user.click(screen.getByLabelText('Load taste-peer interests'));

    expect(await screen.findByText('dub')).toBeInTheDocument();
    expect(screen.getByText('noise')).toBeInTheDocument();
    expect(soulseekDiscovery.getUserInterests).toHaveBeenCalledWith({
      username: 'taste-peer',
    });
  });

  it('treats malformed similar-user payloads as an empty list', async () => {
    const user = userEvent.setup();
    soulseekDiscovery.getSimilarUsers.mockResolvedValue({
      data: { username: 'not-a-list' },
    });

    render(<SoulseekDiscoveryPanel />);

    await user.click(screen.getByRole('button', { name: 'Similar Users' }));

    expect(await screen.findByText('Loaded 0 similar users.')).toBeInTheDocument();
    expect(screen.queryByText('not-a-list')).not.toBeInTheDocument();
  });
});
