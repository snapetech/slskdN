// <copyright file="DownloadDestinationSelector.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as destinations from '../../lib/destinations';
import DownloadDestinationSelector, {
  DOWNLOAD_DESTINATION_STORAGE_KEY,
  chooseDownloadDestination,
} from './DownloadDestinationSelector';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';

vi.mock('../../lib/destinations', () => ({
  getAll: vi.fn(),
}));

const configured = [
  {
    exists: true,
    isDefault: false,
    name: 'Downloads',
    path: '/downloads',
  },
  {
    exists: true,
    isDefault: true,
    name: 'Music',
    path: '/music',
  },
];

describe('DownloadDestinationSelector', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    destinations.getAll.mockResolvedValue(configured);
  });

  it('uses the configured default when the browser has no valid preference', async () => {
    const onChange = vi.fn();

    render(<DownloadDestinationSelector onChange={onChange} />);

    await waitFor(() => expect(onChange).toHaveBeenCalledWith('/music'));
    expect(screen.getByRole('listbox', { name: 'Download destination' }))
      .toHaveTextContent('Music (default)');
  });

  it('restores and updates the browser destination preference', async () => {
    localStorage.setItem(DOWNLOAD_DESTINATION_STORAGE_KEY, '/downloads');
    const onChange = vi.fn();
    const user = userEvent.setup();

    render(<DownloadDestinationSelector onChange={onChange} />);

    await waitFor(() => expect(onChange).toHaveBeenCalledWith('/downloads'));
    await user.click(screen.getByRole('listbox', { name: 'Download destination' }));
    await user.click(screen.getByText('Music (default)'));

    expect(localStorage.getItem(DOWNLOAD_DESTINATION_STORAGE_KEY)).toBe('/music');
    expect(onChange).toHaveBeenLastCalledWith('/music');
  });

  it('falls back predictably when destination data is malformed', () => {
    expect(chooseDownloadDestination(undefined, '/missing')).toBeUndefined();
    expect(chooseDownloadDestination(configured, '/missing')).toBe('/music');
  });
});
