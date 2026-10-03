// <copyright file="SongIDPanel.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as songId from '../../lib/songid';
import SongIDPanel from './SongIDPanel';
import useFeatureGates from '../Shared/useFeatureGates';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/songid', () => ({
  createHub: vi.fn(),
}));
vi.mock('../Shared/useFeatureGates', () => ({
  default: vi.fn(),
  isFeatureEnabled: (featureGates, featureId) =>
    featureGates?.[featureId]?.enabled !== false,
}));

describe('SongIDPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the SongID gate without opening its SignalR connection', () => {
    useFeatureGates.mockReturnValue({
      featureGates: {
        songId: { enabled: false, message: 'Experimental feature is disabled.' },
      },
      ready: true,
    });

    render(<SongIDPanel />);

    expect(screen.getByText('SongID is disabled')).toBeInTheDocument();
    expect(screen.getByText('feature.SongId')).toBeInTheDocument();
    expect(songId.createHub).not.toHaveBeenCalled();
  });
});
