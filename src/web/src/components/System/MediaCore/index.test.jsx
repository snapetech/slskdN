// <copyright file="index.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as mediacore from '../../../lib/mediacore';
import MediaCore from './index';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/mediacore', () => ({
  getConflictStrategies: vi.fn(),
  getContentIdStats: vi.fn(),
  getSupportedHashAlgorithms: vi.fn(),
}));

vi.mock('react-toastify', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('MediaCore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mediacore.getContentIdStats.mockResolvedValue({
      mappingsByDomain: {},
      totalDomains: 0,
      totalMappings: 0,
    });
    mediacore.getSupportedHashAlgorithms.mockResolvedValue({
      algorithms: [],
      descriptions: {},
    });
    mediacore.getConflictStrategies.mockResolvedValue([]);
  });

  it('renders a pod workflow index with safety framing', async () => {
    render(<MediaCore />);

    expect(await screen.findByText('Pod Workflow Index')).toBeInTheDocument();
    expect(screen.getByText(/Pod workflows mix read-only diagnostics/)).toBeInTheDocument();
    expect(screen.getByText('Workflow focus')).toBeInTheDocument();
    expect(screen.getAllByText('Show all pod workflows').length).toBeGreaterThan(0);
    expect(screen.getAllByText('DHT Publishing').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Verification').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Signing').length).toBeGreaterThan(0);
    expect(screen.getByText('Publishes metadata')).toBeInTheDocument();
    expect(screen.getAllByText('Handles key material').length).toBeGreaterThan(0);
    expect(screen.getByText('Read-only verification')).toBeInTheDocument();
    expect(screen.getByText('Publishes pod metadata')).toBeInTheDocument();
    expect(screen.getByText('Mutates local message storage')).toBeInTheDocument();
    expect(screen.getAllByText('Publishes opinion data').length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /DHT Publishing/ })).toHaveAttribute(
      'href',
      '#podcore-dht-publishing',
    );
  });

  it('focuses a pod workflow from the index card', async () => {
    render(<MediaCore />);

    fireEvent.click(await screen.findByRole('link', { name: /DHT Publishing/ }));

    expect(
      screen.getByText(/Showing DHT Publishing/),
    ).toBeInTheDocument();

    fireEvent.click(screen.getAllByText('Show all pod workflows').at(-1));

    expect(screen.queryByText(/Showing DHT Publishing/)).not.toBeInTheDocument();
  });
});
