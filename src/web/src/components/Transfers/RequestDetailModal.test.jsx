// <copyright file="RequestDetailModal.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as downloadRequests from '../../lib/downloadRequests';
import RequestDetailModal from './RequestDetailModal';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/downloadRequests', () => ({
  cancel: vi.fn(),
  get: vi.fn(),
  rename: vi.fn(),
}));

describe('RequestDetailModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    downloadRequests.get.mockResolvedValue({
      attempts: [],
      request: {
        createdAt: '2026-10-06T00:00:00.000Z',
        id: 'request-1',
        name: 'Track request',
        state: 'Pending',
      },
    });
  });

  it('explains save, cancel, and close actions', async () => {
    render(<RequestDetailModal onClose={vi.fn()} open requestId="request-1" />);

    const save = await screen.findByRole('button', { name: 'Save' });
    expect(save).toBeDisabled();
    fireEvent.mouseEnter(save.parentElement);
    expect(await screen.findByText('Save the changed display label for this request.'))
      .toBeInTheDocument();

    const cancel = screen.getByRole('button', { name: 'Cancel request' });
    fireEvent.mouseEnter(cancel);
    expect(await screen.findByText('Cancel the current attempt and mark this request cancelled.'))
      .toBeInTheDocument();

    const close = screen.getByRole('button', { name: 'Close' });
    fireEvent.mouseEnter(close);
    expect(await screen.findByText('Close these request details and return to transfers.'))
      .toBeInTheDocument();
  });
});
