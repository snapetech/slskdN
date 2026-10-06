import ShrinkableButton from './ShrinkableButton';
import React from 'react';
import { useMediaQuery } from 'react-responsive';
import userEvent from '@testing-library/user-event';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('react-responsive', () => ({ useMediaQuery: vi.fn() }));

describe('ShrinkableButton', () => {
  beforeEach(() => {
    useMediaQuery.mockReturnValue(false);
  });

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

  it('keeps the action name and tooltip available at wide and narrow widths', async () => {
    const tooltip = 'Recount files in configured shares after changing files or exclusions.';
    const props = {
      icon: 'refresh',
      mediaQuery: '(max-width: 516px)',
      onClick: vi.fn(),
      tooltip,
    };
    const { rerender } = render(
      <ShrinkableButton {...props}>Rescan Shares</ShrinkableButton>,
    );

    let button = screen.getByRole('button', { name: 'Rescan Shares' });
    expect(button).toHaveTextContent('Rescan Shares');
    await userEvent.setup().hover(button);
    expect(await screen.findByText(tooltip)).toBeInTheDocument();

    useMediaQuery.mockReturnValue(true);
    rerender(<ShrinkableButton {...props}>Rescan Shares</ShrinkableButton>);

    button = screen.getByRole('button', { name: 'Rescan Shares' });
    expect(button).not.toHaveTextContent('Rescan Shares');
    await userEvent.setup().hover(button);
    expect(await screen.findByText(tooltip)).toBeInTheDocument();
  });
});
