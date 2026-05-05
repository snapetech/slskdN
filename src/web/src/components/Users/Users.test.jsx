import * as users from '../../lib/users';
import Users from './Users';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { MemoryRouter, Route, Routes, useNavigate } from 'react-router-dom';

vi.mock('../../lib/users', () => ({
  getEndpoint: vi.fn(),
  getInfo: vi.fn(),
  getStatus: vi.fn(),
}));

vi.mock('./User', () => ({
  default: ({ username }) => <div>User profile: {username}</div>,
}));

describe('Users', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    users.getInfo.mockResolvedValue({ data: { username: 'alice' } });
    users.getStatus.mockResolvedValue({ data: {} });
    users.getEndpoint.mockResolvedValue({ data: {} });
  });

  it('opens a user profile from a URL so profile actions work in new tabs', async () => {
    render(
      <MemoryRouter initialEntries={['/users?user=alice']}>
        <Users />
      </MemoryRouter>,
    );

    expect(await screen.findByText('User profile: alice')).toBeInTheDocument();
    expect(users.getInfo).toHaveBeenCalledWith({ username: 'alice' });
  });

  it('observes URL user changes while the page remains mounted', async () => {
    users.getInfo.mockImplementation(({ username }) =>
      Promise.resolve({ data: { username } }),
    );

    const Harness = () => {
      const navigate = useNavigate();
      return (
        <>
          <button onClick={() => navigate('/users?user=bob')}>open bob</button>
          <Users />
        </>
      );
    };

    render(
      <MemoryRouter initialEntries={['/users?user=alice']}>
        <Routes>
          <Route
            element={<Harness />}
            path="/users"
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('User profile: alice')).toBeInTheDocument();
    screen.getByText('open bob').click();
    expect(await screen.findByText('User profile: bob')).toBeInTheDocument();
    expect(users.getInfo).toHaveBeenCalledWith({ username: 'bob' });
  });
});
