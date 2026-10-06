// <copyright file="MediaCoreButton.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import MediaCoreButton from './MediaCoreButton';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';

describe('MediaCoreButton', () => {
  it('keeps explicit guidance available while a button is disabled', async () => {
    const onClick = vi.fn();
    render(
      <MediaCoreButton
        disabled
        onClick={onClick}
        tooltip="Load published metadata stats to review recent DHT operations."
      >
        Load Pod Publishing Stats
      </MediaCoreButton>,
    );

    const button = screen.getByRole('button', {
      name: 'Load Pod Publishing Stats',
    });
    expect(button).toBeDisabled();
    fireEvent.mouseEnter(button.parentElement);

    expect(
      await screen.findByText(
        'Load published metadata stats to review recent DHT operations.',
      ),
    ).toBeInTheDocument();
    expect(onClick).not.toHaveBeenCalled();
  });
});
