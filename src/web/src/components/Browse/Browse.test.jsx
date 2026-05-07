import Browse from './Browse';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('./BrowseSession', () => ({
  default: ({ username }) => (
    <div data-testid="browse-session">{username || 'empty'}</div>
  ),
}));

describe('Browse', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('opens a user browse tab from a URL so search result links work in new tabs', () => {
    render(
      <MemoryRouter initialEntries={['/browse?user=alice']}>
        <Browse />
      </MemoryRouter>,
    );

    expect(screen.getByTestId('browse-session')).toHaveTextContent('alice');
    expect(screen.queryByText('empty')).not.toBeInTheDocument();
  });

  it('opens a user browse tab from router state for in-app navigation', () => {
    render(
      <MemoryRouter initialEntries={[{ pathname: '/browse', state: { user: 'bob' } }]}>
        <Browse />
      </MemoryRouter>,
    );

    expect(screen.getByTestId('browse-session')).toHaveTextContent('bob');
  });

  it('ignores corrupted persisted tab shapes instead of crashing', () => {
    localStorage.setItem('slskd-browse-tabs', JSON.stringify({ tabs: {} }));

    render(
      <MemoryRouter initialEntries={['/browse']}>
        <Browse />
      </MemoryRouter>,
    );

    expect(screen.getByText('New Tab')).toBeInTheDocument();
  });

  it('ignores malformed persisted tab entries and counters', () => {
    localStorage.setItem(
      'slskd-browse-tabs',
      JSON.stringify({
        tabCounter: -1,
        tabs: [
          null,
          'bad',
          { key: 'tab-7', label: [], username: { bad: true } },
        ],
      }),
    );

    render(
      <MemoryRouter initialEntries={['/browse']}>
        <Browse />
      </MemoryRouter>,
    );

    expect(screen.getByText('New Tab')).toBeInTheDocument();
  });
});
