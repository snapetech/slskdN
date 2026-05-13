import System from './System';
import React from 'react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { render, screen } from '@testing-library/react';

vi.mock('./AdminPolicies', () => ({ default: () => null }));
vi.mock('./AutomationCenter', () => ({ default: () => null }));
vi.mock('./Bridge', () => ({ default: () => null }));
vi.mock('./Data', () => ({ default: () => null }));
vi.mock('./Events', () => ({ default: () => null }));
vi.mock('./ExperienceSettings', () => ({ default: () => null }));
vi.mock('./Files', () => ({ default: () => null }));
vi.mock('./Info', () => ({ default: () => <div>System Info Pane</div> }));
vi.mock('./Integrations', () => ({ default: () => null }));
vi.mock('./Jobs', () => ({ default: () => null }));
vi.mock('./LibraryHealth', () => ({ default: () => null }));
vi.mock('./Logs', () => ({ default: () => null }));
vi.mock('./MediaCore', () => ({ default: () => null }));
vi.mock('./Mesh', () => ({ default: () => null }));
vi.mock('./Metrics', () => ({ default: () => null }));
vi.mock('./Network', () => ({ default: () => null }));
vi.mock('./Options', () => ({ default: () => null }));
vi.mock('./QuarantineJury', () => ({ default: () => null }));
vi.mock('./Security', () => ({ default: () => null }));
vi.mock('./Shares', () => ({ default: () => null }));
vi.mock('./SourceProviders', () => ({ default: () => null }));
vi.mock('./SwarmAnalytics', () => ({ default: () => null }));
vi.mock('../Shared', () => ({
  Switch: ({ children }) => <>{children}</>,
}));

const LocationProbe = () => {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
};

describe('System', () => {
  it('redirects unknown system tabs to the default info tab', async () => {
    render(
      <MemoryRouter initialEntries={['/system/not-real']}>
        <Routes>
          <Route
            element={
              <>
                <LocationProbe />
                <System />
              </>
            }
            path="/system/:tab"
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByTestId('location')).toHaveTextContent('/system/info');
  });

  it('labels admin and experimental system panels in the tab menu', async () => {
    render(
      <MemoryRouter initialEntries={['/system/info']}>
        <Routes>
          <Route
            element={<System />}
            path="/system/:tab"
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('MediaCore')).toBeInTheDocument();
    expect(screen.getAllByText('Experimental').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Admin').length).toBeGreaterThan(0);
  });
});
