import PlayCollectionItemButton from './PlayCollectionItemButton';
import { PlayerContext } from './PlayerContext';
import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import { vi } from 'vitest';

describe('PlayCollectionItemButton', () => {
  it('names the collection item action and shows guidance without playing on hover', async () => {
    const user = userEvent.setup();
    const playItem = vi.fn();
    render(
      <PlayerContext.Provider value={{ playItem, playerVisible: true }}>
        <PlayCollectionItemButton item={{ contentId: 'sha256:fixture', title: 'Fixture Track' }} />
      </PlayerContext.Provider>,
    );

    const button = screen.getByRole('button', {
      name: 'Play Fixture Track in the browser player',
    });
    await user.hover(button);

    expect(await screen.findByText(
      'Play this item through the local stream endpoint and update your now-playing status.',
    )).toBeInTheDocument();
    expect(playItem).not.toHaveBeenCalled();
  });
});
