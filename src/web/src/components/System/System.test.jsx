import System from './System';
import React from 'react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { render, screen, within } from '@testing-library/react';

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
  beforeEach(() => {
    Object.defineProperty(Element.prototype, 'scrollIntoView', {
      configurable: true,
      value: vi.fn(),
    });
  });

  afterEach(() => {
    delete Element.prototype.scrollIntoView;
  });

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

  const renderAtTab = (tab) => render(
    <MemoryRouter initialEntries={[`/system/${tab}`]}>
      <Routes>
        <Route
          element={<System />}
          path="/system/:tab"
        />
      </Routes>
    </MemoryRouter>,
  );

  it('groups the 22 settings tabs into named sections instead of one flat strip', async () => {
    renderAtTab('info');

    [
      'Overview',
      'Network & Mesh',
      'Security & Trust',
      'Automation & Jobs',
      'Diagnostics',
      'Advanced',
    ].forEach((section) => {
      expect(screen.getByText(section)).toBeInTheDocument();
    });

    // Only the active section's tabs render at once — the other 19 aren't
    // dumped into the DOM alongside them.
    expect(await screen.findByText('Info')).toBeInTheDocument();
    expect(screen.getByText('Network')).toBeInTheDocument();
    expect(screen.getByText('Shares')).toBeInTheDocument();
    expect(screen.queryByText('MediaCore')).not.toBeInTheDocument();
    expect(screen.queryByText('Logs')).not.toBeInTheDocument();
  });

  it('labels experimental panels within the Network & Mesh section', async () => {
    renderAtTab('mesh');

    expect(await screen.findByText('MediaCore')).toBeInTheDocument();
    ['Mesh', 'Bridge', 'MediaCore', 'Source Providers', 'Swarm Analytics'].forEach(
      (panel) => {
        expect(
          within(screen.getByText(panel).closest('.item')).getByText('Experimental'),
        ).toBeInTheDocument();
      },
    );
  });

  it('labels admin panels within the section each one belongs to', async () => {
    renderAtTab('policies');
    expect(await screen.findByText('Policies')).toBeInTheDocument();
    ['Policies', 'Quarantine Jury'].forEach((panel) => {
      expect(
        within(screen.getByText(panel).closest('.item')).getByText('Admin'),
      ).toBeInTheDocument();
    });

    renderAtTab('automations');
    expect(await screen.findByText('Automations')).toBeInTheDocument();
    expect(
      within(screen.getByText('Automations').closest('.item')).getByText('Admin'),
    ).toBeInTheDocument();

    renderAtTab('data');
    expect(await screen.findByText('Data')).toBeInTheDocument();
    ['Data', 'Logs', 'Metrics'].forEach((panel) => {
      expect(
        within(screen.getByText(panel).closest('.item')).getByText('Admin'),
      ).toBeInTheDocument();
    });

    renderAtTab('integrations');
    expect(await screen.findByText('Integrations')).toBeInTheDocument();
    ['Integrations', 'Options'].forEach((panel) => {
      expect(
        within(screen.getByText(panel).closest('.item')).getByText('Admin'),
      ).toBeInTheDocument();
    });
  });

  it('reveals the active tab when a later route opens directly', async () => {
    render(
      <MemoryRouter initialEntries={['/system/options']}>
        <Routes>
          <Route
            element={<System />}
            path="/system/:tab"
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('Options')).toBeInTheDocument();
    expect(Element.prototype.scrollIntoView).toHaveBeenCalledWith({
      block: 'nearest',
      inline: 'nearest',
    });
  });
});
