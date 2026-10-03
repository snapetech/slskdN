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
});
