import TooltipButton from './TooltipButton';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

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
});
