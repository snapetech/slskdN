// <copyright file="useFeatureGates.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as slskdn from '../../lib/slskdn';
import useFeatureGates, { isFeatureEnabled } from './useFeatureGates';
import { act, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/slskdn', () => ({
  getCapabilities: vi.fn(),
}));

const FeatureGateProbe = () => {
  const { featureGates, ready } = useFeatureGates();
  return (
    <span>
      {ready
        ? isFeatureEnabled(featureGates, 'pods')
          ? 'enabled'
          : 'disabled'
        : 'loading'}
    </span>
  );
};

describe('useFeatureGates', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
  });

  it('reads effective statuses and refreshes when a hidden page becomes visible', async () => {
    slskdn.getCapabilities
      .mockResolvedValueOnce({
        featureGates: { pods: { enabled: false } },
      })
      .mockResolvedValueOnce({
        featureGates: { pods: { enabled: true } },
      });

    render(<FeatureGateProbe />);

    expect(await screen.findByText('disabled')).toBeInTheDocument();
    expect(slskdn.getCapabilities).toHaveBeenCalledTimes(1);

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });
    act(() => {
      document.dispatchEvent(new Event('visibilitychange'));
    });
    expect(slskdn.getCapabilities).toHaveBeenCalledTimes(1);

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    act(() => {
      document.dispatchEvent(new Event('visibilitychange'));
    });

    await waitFor(() => expect(screen.getByText('enabled')).toBeInTheDocument());
    expect(slskdn.getCapabilities).toHaveBeenCalledTimes(2);
  });

  it('treats a missing status as enabled for older capability responses', async () => {
    slskdn.getCapabilities.mockResolvedValue({ features: [] });

    render(<FeatureGateProbe />);

    await waitFor(() => expect(screen.getByText('enabled')).toBeInTheDocument());
  });

  it('shares one capabilities request between active consumers', async () => {
    slskdn.getCapabilities.mockResolvedValue({
      featureGates: { pods: { enabled: false } },
    });

    render(
      <>
        <FeatureGateProbe />
        <FeatureGateProbe />
      </>,
    );

    await waitFor(() => {
      expect(screen.getAllByText('disabled')).toHaveLength(2);
    });
    expect(slskdn.getCapabilities).toHaveBeenCalledTimes(1);
  });
});
