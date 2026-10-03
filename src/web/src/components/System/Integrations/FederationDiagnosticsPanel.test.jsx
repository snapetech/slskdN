// <copyright file="FederationDiagnosticsPanel.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as federationDiagnostics from '../../../lib/federationDiagnostics';
import FederationDiagnosticsPanel from './FederationDiagnosticsPanel';
import useFeatureGates from '../../Shared/useFeatureGates';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/federationDiagnostics', () => ({
  getDiagnostics: vi.fn(),
}));
vi.mock('../../Shared/useFeatureGates', () => ({
  default: vi.fn(),
  isFeatureEnabled: (featureGates, featureId) =>
    featureGates?.[featureId]?.enabled !== false,
}));

describe('FederationDiagnosticsPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('explains a disabled federation gate without calling its API', () => {
    useFeatureGates.mockReturnValue({
      featureGates: {
        socialFederation: {
          enabled: false,
          message: 'Experimental feature is disabled.',
        },
      },
      ready: true,
    });

    render(<FederationDiagnosticsPanel />);

    expect(screen.getByText('Social federation is disabled')).toBeInTheDocument();
    expect(screen.getByText('feature.SocialFederation')).toBeInTheDocument();
    expect(federationDiagnostics.getDiagnostics).not.toHaveBeenCalled();
  });
});
