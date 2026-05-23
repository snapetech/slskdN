// <copyright file="index.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as telemetry from '../../../lib/telemetry';
import Metrics from '.';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';

vi.mock('../../../lib/telemetry');

describe('Metrics', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders while the first metrics request is still pending', () => {
    telemetry.getKpiMetrics.mockReturnValue(new Promise(() => {}));

    render(<Metrics />);

    expect(screen.getByText('Prometheus Metrics')).toBeInTheDocument();
    expect(screen.getByText('Loading metrics')).toBeInTheDocument();
  });

  it('renders empty metrics without crashing', async () => {
    telemetry.getKpiMetrics.mockResolvedValue({});

    render(<Metrics />);

    await waitFor(() =>
      expect(screen.getByText(/Updated /u)).toBeInTheDocument(),
    );
    expect(screen.getByText('Prometheus Metrics')).toBeInTheDocument();
  });
});
