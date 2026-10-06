// <copyright file="MediaServerPanel.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import MediaServerPanel from './MediaServerPanel';
import React from 'react';
import {
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('MediaServerPanel', () => {
  it('renders named provider selectors from adapter labels', () => {
    render(<MediaServerPanel />);

    expect(
      screen.getByRole('button', { name: 'Review Plex sync readiness' }),
    ).toHaveTextContent('Plex');
    expect(
      screen.getByRole('button', {
        name: 'Review Jellyfin / Emby sync readiness',
      }),
    ).toHaveTextContent('Jellyfin / Emby');
    expect(
      screen.getByRole('button', { name: 'Review Navidrome sync readiness' }),
    ).toHaveTextContent('Navidrome');
  });

  it('shows guidance when path values are missing instead of an empty table', () => {
    render(<MediaServerPanel />);

    fireEvent.click(screen.getByRole('button', { name: 'Path Diagnostic' }));

    expect(screen.getByText('Incomplete')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Enter both paths to check whether slskdN and the media server agree.',
      ),
    ).toBeInTheDocument();
  });

  it('builds local path, sync, and contract reviews from the entered values', () => {
    render(<MediaServerPanel />);

    fireEvent.change(
      screen.getByRole('textbox', { name: 'Media server base URL' }),
      { target: { value: 'https://media.example' } },
    );
    fireEvent.click(
      screen.getByRole('checkbox', { name: 'API token is configured' }),
    );
    fireEvent.change(
      screen.getByRole('textbox', {
        name: 'Completed-download path on slskdN',
      }),
      { target: { value: '/downloads/music/album.flac' } },
    );
    fireEvent.change(
      screen.getByRole('textbox', {
        name: 'Library path on the media server',
      }),
      { target: { value: '/library/music/album.flac' } },
    );
    fireEvent.change(
      screen.getByRole('textbox', { name: 'Remote path mapping: from' }),
      { target: { value: '/downloads' } },
    );
    fireEvent.change(
      screen.getByRole('textbox', { name: 'Remote path mapping: to' }),
      { target: { value: '/library' } },
    );
    fireEvent.click(
      screen.getByRole('checkbox', {
        name: 'Media server user mapping is configured',
      }),
    );

    fireEvent.click(screen.getByRole('button', { name: 'Path Diagnostic' }));
    expect(screen.getByText('Mapped')).toBeInTheDocument();
    expect(
      screen.getByText('/library/music/album.flac', { selector: 'code' }),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Preview Sync' }));
    expect(
      screen.getByText(/slskdN media-server sync review/),
    ).toHaveTextContent('Status: Ready for live adapter');

    fireEvent.click(screen.getByRole('button', { name: 'Review Contract' }));
    expect(
      screen.getByText(/slskdN media-server execution contract/),
    ).toHaveTextContent('Status: Execution contract ready');
    expect(
      screen.getByText(/slskdN media-server execution contract/),
    ).toHaveTextContent('Enabled automations ready: 2/2');
  });

  it('explains that review actions stay local', async () => {
    render(<MediaServerPanel />);

    const previewButton = screen.getByRole('button', { name: 'Preview Sync' });
    fireEvent.mouseEnter(previewButton);

    await waitFor(() => {
      expect(
        screen.getByText(
          'Check URL, token, and path readiness. This creates a local report and does not contact the media server.',
        ),
      ).toBeInTheDocument();
    });
  });
});
