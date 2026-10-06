import TooltipButton from './TooltipButton';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

describe('TooltipButton', () => {
  it('shows its Popup guidance while the button is disabled', async () => {
    render(
      <TooltipButton disabled tooltip="Enable Solid before resolving a WebID.">
        Resolve WebID
      </TooltipButton>,
    );

    const button = screen.getByRole('button', { name: 'Resolve WebID' });
    expect(button).toBeDisabled();
    fireEvent.mouseEnter(button.parentElement);

    await waitFor(() => {
      expect(screen.getByText('Enable Solid before resolving a WebID.'))
        .toBeInTheDocument();
    });
  });

  it('removes the disabled trigger when async work enables the button', () => {
    const { rerender } = render(
      <TooltipButton disabled tooltip="Load the latest data from the server.">
        Refresh
      </TooltipButton>,
    );

    rerender(
      <TooltipButton tooltip="Load the latest data from the server.">
        Refresh
      </TooltipButton>,
    );

    expect(screen.getAllByRole('button', { name: 'Refresh' })).toHaveLength(1);
    expect(screen.getByRole('button', { name: 'Refresh' })).toBeEnabled();
  });

  it('uses tooltip copy as the accessible name for icon-only actions', async () => {
    const onClick = vi.fn();
    const tooltip = 'Refresh the current peer list from local-network discovery.';
    render(
      <TooltipButton icon="refresh" onClick={onClick} tooltip={tooltip} />,
    );

    const button = screen.getByRole('button', { name: tooltip });
    fireEvent.mouseEnter(button);

    await waitFor(() => {
      expect(screen.getByText(tooltip)).toBeInTheDocument();
    });
    expect(onClick).not.toHaveBeenCalled();
  });

  it('keeps Semantic UI content as the accessible name when tooltip copy is longer', async () => {
    const tooltip = "Fetch Lidarr's missing albums and add new ones to the slskdN wishlist.";
    render(
      <TooltipButton
        content="Sync Wanted Now"
        icon="sync"
        tooltip={tooltip}
      />,
    );

    const button = screen.getByRole('button', { name: 'Sync Wanted Now' });
    fireEvent.mouseEnter(button);

    await waitFor(() => {
      expect(screen.getByText(tooltip)).toBeInTheDocument();
    });
  });
});
