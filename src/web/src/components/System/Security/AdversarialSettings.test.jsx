// <copyright file="AdversarialSettings.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as securityApi from '../../../lib/security';
import AdversarialSettings from './AdversarialSettings';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';

vi.mock('../../../lib/security', () => ({
  getAdversarialSettings: vi.fn(),
  getAdversarialStats: vi.fn(),
  getTorStatus: vi.fn(),
  getTransportStatus: vi.fn(),
  testTorConnectivity: vi.fn(),
  testTransportConnectivity: vi.fn(),
  updateAdversarialSettings: vi.fn(),
}));

describe('Adversarial Settings tabs', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    securityApi.getAdversarialSettings.mockResolvedValue({
      Anonymity: { Enabled: false },
      Enabled: false,
      Privacy: { Enabled: false },
      Profile: 'Disabled',
      Transport: { Enabled: false },
    });
    securityApi.getAdversarialStats.mockResolvedValue(null);
    securityApi.getTorStatus.mockResolvedValue(null);
    securityApi.getTransportStatus.mockResolvedValue(null);
  });

  it('renders the overview content beneath the tab menu', async () => {
    render(<AdversarialSettings />);

    expect(
      await screen.findByText('Adversarial Resilience Overview'),
    ).toBeInTheDocument();
    expect(securityApi.getTransportStatus).not.toHaveBeenCalled();
    expect(securityApi.getTorStatus).not.toHaveBeenCalled();
  });

  it('loads configured transport status without probing disabled Tor', async () => {
    securityApi.getAdversarialSettings.mockResolvedValue({
      Anonymity: { Enabled: false, Mode: 'Direct' },
      Enabled: true,
      Transport: { Enabled: true },
    });

    render(<AdversarialSettings />);

    await waitFor(() =>
      expect(securityApi.getTransportStatus).toHaveBeenCalledTimes(1),
    );
    expect(securityApi.getTorStatus).not.toHaveBeenCalled();
  });

  it('loads Tor status when Tor anonymity is enabled', async () => {
    securityApi.getAdversarialSettings.mockResolvedValue({
      Anonymity: { Enabled: true, Mode: 'Tor' },
      Enabled: true,
      Transport: { Enabled: true },
    });

    render(<AdversarialSettings />);

    await waitFor(() =>
      expect(securityApi.getTorStatus).toHaveBeenCalledTimes(1),
    );
    expect(securityApi.getTransportStatus).toHaveBeenCalledTimes(1);
  });
});
