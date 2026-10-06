import '@testing-library/jest-dom';
import LoginForm, { getHttpsHintUrl } from './LoginForm';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('./Shared/Footer', () => ({ default: () => <div>Footer</div> }));

describe('LoginForm', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('builds an HTTPS hint when the page is loaded over HTTP', () => {
    expect(getHttpsHintUrl(new URL('http://slskdn.local:5030/'))).toBe(
      'https://slskdn.local:5031',
    );
  });

  it('returns no HTTPS hint when the page is already loaded over HTTPS', () => {
    expect(getHttpsHintUrl(new URL('https://slskdn.local:5031/'))).toBeNull();
  });

  it('renders the HTTPS hint in the login form when served over HTTP', () => {
    window.history.pushState({}, '', '/');

    render(
      <LoginForm
        loading={false}
        onLoginAttempt={vi.fn()}
      />,
    );

    const link = screen.getByRole('link', { name: 'https://localhost:5031' });
    expect(link).toHaveAttribute('href', 'https://localhost:5031');
    expect(screen.getByText('HTTPS Option')).toBeInTheDocument();
  });

  it('explains why sign-in is unavailable before credentials are entered', async () => {
    const onLoginAttempt = vi.fn();
    render(<LoginForm loading={false} onLoginAttempt={onLoginAttempt} />);

    const submit = screen.getByRole('button', { name: 'Login' });
    expect(submit).toBeDisabled();
    fireEvent.mouseEnter(submit.parentElement);

    await waitFor(() => {
      expect(screen.getByText(
        'Sign in with these credentials to open your authenticated dashboard.',
      )).toBeInTheDocument();
    });
    expect(onLoginAttempt).not.toHaveBeenCalled();
  });
});
