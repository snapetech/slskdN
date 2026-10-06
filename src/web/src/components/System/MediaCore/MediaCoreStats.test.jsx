// <copyright file="MediaCoreStats.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as mediacore from '../../../lib/mediacore';
import MediaCoreStats from './MediaCoreStats';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/mediacore', () => ({
  clearRetrievalCache: vi.fn(),
  getContentPublishingStats: vi.fn(),
  getContentRegistryStats: vi.fn(),
  getDescriptorStats: vi.fn(),
  getFuzzyMatchingStats: vi.fn(),
  getIpldMappingStats: vi.fn(),
  getMediaCoreDashboard: vi.fn(),
  getMetadataPortabilityStats: vi.fn(),
  getPerceptualHashingStats: vi.fn(),
  getRetrievalStats: vi.fn(),
  resetMediaCoreStats: vi.fn(),
}));

vi.mock('react-toastify', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('MediaCoreStats', () => {
  it('explains what each statistics action loads or changes', async () => {
    render(<MediaCoreStats />);
    fireEvent.click(screen.getByText('Advanced retrieval cache controls'));
    fireEvent.click(screen.getByText('Advanced dashboard reset controls'));

    const actions = [
      [
        'Load Stats',
        'Load descriptor retrieval performance and cache statistics.',
      ],
      [
        'Clear Cache',
        'Remove cached descriptor lookups. Later DHT retrievals may generate more peer traffic.',
      ],
      [
        'Load Full Dashboard',
        'Load overall MediaCore performance and usage metrics.',
      ],
      [
        'Reset All Stats',
        'Clear all accumulated MediaCore statistics after confirming.',
      ],
      [
        'Load Registry Stats',
        'Load Content Registry mapping totals and domain statistics.',
      ],
      [
        'Load Descriptor Stats',
        'Load descriptor retrieval and cache performance metrics.',
      ],
      [
        'Load Fuzzy Stats',
        'Load fuzzy-match totals, success rate, and accuracy metrics.',
      ],
      [
        'Load Hashing Stats',
        'Load perceptual-hash computation and accuracy metrics.',
      ],
      [
        'Load IPLD Stats',
        'Load IPLD node, link, and mapping statistics.',
      ],
      [
        'Load Portability Stats',
        'Load metadata export, import, and conflict-resolution metrics.',
      ],
      [
        'Load Publishing Stats',
        'Load Content Publishing and DHT publication performance metrics.',
      ],
    ];

    for (const [label, tooltip] of actions) {
      const button = screen.getByRole('button', { name: label });
      fireEvent.mouseEnter(button);
      expect(await screen.findByText(tooltip)).toBeInTheDocument();
      fireEvent.mouseLeave(button);
    }

    expect(mediacore.clearRetrievalCache).not.toHaveBeenCalled();
    expect(mediacore.resetMediaCoreStats).not.toHaveBeenCalled();
  });
});
