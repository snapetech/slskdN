// <copyright file="MediaServerPanel.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import MediaServerPanel from './MediaServerPanel';
import React from 'react';
import { render, screen } from '@testing-library/react';
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
});
