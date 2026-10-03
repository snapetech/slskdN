import ShrinkableButton from './ShrinkableButton';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

describe('ShrinkableButton', () => {
  it('shows tooltip guidance when its button is disabled', async () => {
    render(
      <ShrinkableButton
        disabled
        icon="lock"
        mediaQuery="(max-width: 516px)"
        tooltip="Remote YAML editing is disabled in server configuration."
      >
        Remote Configuration Disabled
      </ShrinkableButton>,
    );

    const button = screen.getByRole('button', { name: 'Remote Configuration Disabled' });
    expect(button).toBeDisabled();
    fireEvent.mouseEnter(button.parentElement);

    await waitFor(() => {
      expect(screen.getByText('Remote YAML editing is disabled in server configuration.'))
        .toBeInTheDocument();
    });
  });
});
