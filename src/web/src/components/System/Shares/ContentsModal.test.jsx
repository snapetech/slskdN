import { browse } from '../../../lib/shares';
import ContentsModal from './ContentsModal';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';

vi.mock('../../../lib/shares', () => ({
  browse: vi.fn(),
}));

vi.mock('../../Shared', () => ({
  CodeEditor: ({ value }) => (
    <textarea
      aria-label="Share contents"
      readOnly
      value={value || ''}
    />
  ),
  LoaderSegment: () => <div>Loading</div>,
  Switch: ({ children, loading }) => loading || children,
}));

describe('ContentsModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('sorts a copied file list without mutating browse results', async () => {
    const files = [
      { filename: '/remote/music/z.flac' },
      { filename: '/remote/music/a.flac' },
    ];
    browse.mockResolvedValue([
      {
        files,
        name: '/remote/music',
      },
    ]);

    render(
      <ContentsModal
        onClose={vi.fn()}
        share={{
          id: 'share-1',
          localPath: '/music',
          remotePath: '/remote/music',
        }}
        theme="dark"
      />,
    );

    await waitFor(() =>
      expect(screen.getByLabelText('Share contents')).toHaveValue(
        '/music\n\t/a.flac\n\t/z.flac\n',
      ),
    );
    expect(files.map((file) => file.filename)).toEqual([
      '/remote/music/z.flac',
      '/remote/music/a.flac',
    ]);
  });
});
