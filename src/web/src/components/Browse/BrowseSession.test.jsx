import * as users from '../../lib/users';
import BrowseSession from './BrowseSession';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';

vi.mock('../../lib/users', () => ({
  browse: vi.fn(),
  getBrowseStatus: vi.fn(),
}));

vi.mock('../../lib/userNotes', () => ({
  getNote: vi.fn(() => Promise.reject(new Error('no note'))),
}));

vi.mock('../../lib/transfers', () => ({
  download: vi.fn(),
}));

vi.mock('../Shared/UserCard', () => ({
  default: ({ children }) => <span>{children}</span>,
}));

vi.mock('../Users/UserNoteModal', () => ({
  default: () => null,
}));

describe('BrowseSession', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    users.getBrowseStatus.mockRejectedValue({ response: { status: 404 } });
  });

  it('shows the server browse failure reason', async () => {
    users.browse.mockRejectedValue({
      response: {
        data: 'Unable to browse user; the remote peer is unavailable',
        status: 503,
      },
    });

    render(<BrowseSession username="alice" />);

    expect(
      await screen.findByText(
        'Failed to browse alice: Unable to browse user; the remote peer is unavailable',
      ),
    ).toBeInTheDocument();

    await waitFor(() => expect(users.browse).toHaveBeenCalledWith({ username: 'alice' }));
  });
});
