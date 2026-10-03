import Shares from './index';
import * as sharesLibrary from '../../../lib/shares';
import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
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

  it('opens share contents from a named keyboard action without changing the URL', async () => {
    const share = {
      alias: 'Music',
      directories: 12,
      files: 2400,
      id: 'share-1',
      isExcluded: false,
      localPath: '/library/music',
      remotePath: '/remote/music',
    };
    sharesLibrary.getAll.mockResolvedValue({ node: [share] });
    const user = userEvent.setup();

    render(
      <MemoryRouter initialEntries={['/system/shares']}>
        <LocationProbe />
        <Shares state={{}} />
      </MemoryRouter>,
    );

    const action = await screen.findByRole('button', {
      name: 'View files in share /library/music',
    });
    expect(action).toHaveAttribute(
      'title',
      'Open the file list for share /library/music.',
    );
    action.focus();
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(sharesLibrary.browse).toHaveBeenCalledWith({ id: 'share-1' });
    });
    expect(screen.getByTestId('share-location')).toHaveTextContent('/system/shares');
    expect(document.querySelector('.ui.modal.visible')).toBeInTheDocument();
  });

  it('cancels delayed post-scan refreshes when unmounted', async () => {
    vi.useFakeTimers();
    sharesLibrary.getAll.mockResolvedValue([]);

    const { unmount } = renderShares();

    await waitFor(() => expect(sharesLibrary.getAll).toHaveBeenCalledTimes(2));
    unmount();
    await vi.advanceTimersByTimeAsync(1_000);

    expect(sharesLibrary.getAll).toHaveBeenCalledTimes(2);
    vi.useRealTimers();
  });
});

const LocationProbe = () => {
  const location = useLocation();
  return (
    <div data-testid="share-location">
      {location.pathname}{location.search}{location.hash}
    </div>
  );
};
  const renderShares = () =>
    render(
      <MemoryRouter>
        <Shares state={{}} />
      </MemoryRouter>,
    );
