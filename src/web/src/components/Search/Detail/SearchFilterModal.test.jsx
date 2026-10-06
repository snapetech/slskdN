import SearchFilterModal from './SearchFilterModal';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

describe('SearchFilterModal', () => {
  it('explains each preset and modal action', async () => {
    render(
      <SearchFilterModal
        filterString=""
        onChange={vi.fn()}
        trigger={<button type="button">Open filters</button>}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Open filters' }));

    const tooltips = [
      ['High Quality (320kbps+)', 'Set a 320 kbps minimum and prefer lossy results for a high-bitrate match.'],
      ['Lossless Only', 'Require lossless files with at least 16-bit, 44.1 kHz audio.'],
      ['Clear Quality', 'Clear the bitrate, lossless, bit-depth, and sample-rate filters.'],
      ['Cancel', 'Close without applying these search filters.'],
      ['Apply Filters', 'Apply these filters to the current search.'],
    ];

    for (const [label, tooltip] of tooltips) {
      const button = await screen.findByRole('button', { name: label });
      fireEvent.mouseEnter(button);
      expect(await screen.findByText(tooltip)).toBeInTheDocument();
    }
  });
});
