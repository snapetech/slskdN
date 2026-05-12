import * as files from '../../../lib/files';
import Explorer from './Explorer';
import { render, waitFor } from '@testing-library/react';
import React from 'react';

vi.mock('../../../lib/files', () => ({
  deleteDirectory: vi.fn(),
  deleteFile: vi.fn(),
  list: vi.fn(),
}));

describe('System Files Explorer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    files.list.mockResolvedValue({ directories: [], files: [] });
  });

  it('does not fetch file listings while inactive', () => {
    render(
      <Explorer
        active={false}
        remoteFileManagement={false}
        root="incomplete"
      />,
    );

    expect(files.list).not.toHaveBeenCalled();
  });

  it('fetches file listings when activated', async () => {
    const { rerender } = render(
      <Explorer
        active={false}
        remoteFileManagement={false}
        root="incomplete"
      />,
    );

    rerender(
      <Explorer
        active
        remoteFileManagement={false}
        root="incomplete"
      />,
    );

    await waitFor(() =>
      expect(files.list).toHaveBeenCalledWith({
        root: 'incomplete',
        subdirectory: '',
      }),
    );
  });
});
