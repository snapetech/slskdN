import Shares from './index';
import * as sharesLibrary from '../../../lib/shares';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';

vi.mock('../../../lib/shares', () => ({
  browse: vi.fn(),
  cancel: vi.fn(),
  getAll: vi.fn(),
  rescan: vi.fn(),
}));

describe('System Shares', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sharesLibrary.browse.mockResolvedValue([]);
    sharesLibrary.cancel.mockResolvedValue({});
    sharesLibrary.rescan.mockResolvedValue({});
  });

  it('renders an empty table for malformed share host maps', async () => {
    sharesLibrary.getAll.mockResolvedValue([]);

    renderShares();

    expect(await screen.findByText('No shares configured')).toBeInTheDocument();
  });

  it('skips malformed per-host share lists', async () => {
    sharesLibrary.getAll.mockResolvedValue({
      host1: { localPath: '/bad' },
      host2: [
        {
          alias: 'Music',
          directories: 1,
          files: 2,
          localPath: '/music',
          remotePath: '/remote/music',
        },
      ],
    });

    renderShares();

    expect(await screen.findByText('/music')).toBeInTheDocument();
    expect(screen.queryByText('/bad')).not.toBeInTheDocument();
  });
});
  const renderShares = () =>
    render(
      <MemoryRouter>
        <Shares state={{}} />
      </MemoryRouter>,
    );
