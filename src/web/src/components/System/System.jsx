import './System.css';
import { Switch } from '../Shared';
import React, { Suspense, lazy } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { Icon, Menu, Message, Segment, Tab } from 'semantic-ui-react';

const AdminPolicies = lazy(() => import('./AdminPolicies'));
const AutomationCenter = lazy(() => import('./AutomationCenter'));
const Bridge = lazy(() => import('./Bridge'));
const Data = lazy(() => import('./Data'));
const Events = lazy(() => import('./Events'));
const ExperienceSettings = lazy(() => import('./ExperienceSettings'));
const Files = lazy(() => import('./Files'));
const Info = lazy(() => import('./Info'));
const Integrations = lazy(() => import('./Integrations'));
const Jobs = lazy(() => import('./Jobs'));
const LibraryHealth = lazy(() => import('./LibraryHealth'));
const Logs = lazy(() => import('./Logs'));
const MediaCore = lazy(() => import('./MediaCore'));
const Mesh = lazy(() => import('./Mesh'));
const Metrics = lazy(() => import('./Metrics'));
const Network = lazy(() => import('./Network'));
const Options = lazy(() => import('./Options'));
const QuarantineJury = lazy(() => import('./QuarantineJury'));
const Security = lazy(() => import('./Security'));
const Shares = lazy(() => import('./Shares'));
const SourceProviders = lazy(() => import('./SourceProviders'));
const SwarmAnalytics = lazy(() => import('./SwarmAnalytics'));

const renderPane = (Component, props = {}, className) => (
  <Tab.Pane className={className}>
    <Suspense
      fallback={
        <Message info>
          <Icon
            loading
            name="spinner"
          />
          Loading
        </Message>
      }
    >
      <Component {...props} />
    </Suspense>
  </Tab.Pane>
);

const System = ({ options = {}, state = {}, theme }) => {
  const navigate = useNavigate();
  const { tab } = useParams();

  const panes = [
    {
      menuItem: (
        <Menu.Item key="info">
          <Switch
            pending={
              ((state?.pendingRestart ?? false) ||
                (state?.pendingReconnect ?? false)) && (
                <Icon
                  color="yellow"
                  name="exclamation circle"
                />
              )
            }
          >
            <Icon name="info circle" />
          </Switch>
          Info
        </Menu.Item>
      ),
      render: () => renderPane(Info, { options, state, theme }),
      route: 'info',
    },
    {
      menuItem: (
        <Menu.Item key="network">
          <Icon
            color="blue"
            name="sitemap"
          />
          Network
        </Menu.Item>
      ),
      render: () => renderPane(Network, { theme }),
      route: 'network',
    },
    {
      menuItem: {
        content: 'Mesh',
        icon: 'share alternate',
        key: 'mesh',
      },
      render: () => renderPane(Mesh),
      route: 'mesh',
    },
    {
      menuItem: {
        content: 'Bridge',
        icon: 'exchange',
        key: 'bridge',
      },
      render: () => renderPane(Bridge),
      route: 'bridge',
    },
    {
      menuItem: {
        content: 'MediaCore',
        icon: 'music',
        key: 'mediacore',
      },
      render: () => renderPane(MediaCore),
      route: 'mediacore',
    },
    {
      menuItem: {
        content: 'Security',
        icon: 'shield alternate',
        key: 'security',
      },
      render: () => renderPane(Security),
      route: 'security',
    },
    {
      menuItem: {
        content: 'Policies',
        icon: 'sliders horizontal',
        key: 'policies',
      },
      render: () => renderPane(AdminPolicies, { options }, 'full-height'),
      route: 'policies',
    },
    {
      menuItem: {
        content: 'Experience',
        icon: 'compass',
        key: 'experience',
      },
      render: () => renderPane(ExperienceSettings, {}, 'full-height'),
      route: 'experience',
    },
    {
      menuItem: {
        content: 'Integrations',
        icon: 'plug',
        key: 'integrations',
      },
      render: () => renderPane(Integrations, { options, state }, 'full-height'),
      route: 'integrations',
    },
    {
      menuItem: {
        content: 'Options',
        icon: 'options',
        key: 'options',
      },
      render: () => renderPane(Options, { options, theme }, 'full-height'),
      route: 'options',
    },
    {
      menuItem: (
        <Menu.Item key="shares">
          <Switch
            scanPending={
              (state?.shares?.scanPending ?? false) && (
                <Icon
                  color="yellow"
                  name="exclamation circle"
                />
              )
            }
          >
            <Icon name="share external" />
          </Switch>
          Shares
        </Menu.Item>
      ),
      render: () => renderPane(Shares, { state: state.shares, theme }),
      route: 'shares',
    },
    {
      menuItem: {
        content: 'Jobs',
        icon: 'tasks',
        key: 'jobs',
      },
      render: () => renderPane(Jobs, {}, 'full-height'),
      route: 'jobs',
    },
    {
      menuItem: {
        content: 'Automations',
        icon: 'magic',
        key: 'automations',
      },
      render: () => renderPane(AutomationCenter, {}, 'full-height'),
      route: 'automations',
    },
    {
      menuItem: {
        content: 'Source Providers',
        icon: 'random',
        key: 'source-providers',
      },
      render: () => renderPane(SourceProviders, {}, 'full-height'),
      route: 'source-providers',
    },
    {
      menuItem: {
        content: 'Swarm Analytics',
        icon: 'chart line',
        key: 'swarm-analytics',
      },
      render: () => renderPane(SwarmAnalytics, {}, 'full-height'),
      route: 'swarm-analytics',
    },
    {
      menuItem: {
        content: 'Library Health',
        icon: 'heartbeat',
        key: 'library-health',
      },
      render: () => renderPane(LibraryHealth, {}, 'full-height'),
      route: 'library-health',
    },
    {
      menuItem: {
        content: 'Quarantine Jury',
        icon: 'shield',
        key: 'quarantine-jury',
      },
      render: () => renderPane(QuarantineJury, {}, 'full-height'),
      route: 'quarantine-jury',
    },
    {
      menuItem: {
        content: 'Files',
        icon: 'folder open',
        key: 'files',
      },
      render: () => renderPane(Files, { options, theme }, 'full-height'),
      route: 'files',
    },
    {
      menuItem: {
        content: 'Data',
        icon: 'database',
        key: 'data',
      },
      render: () => renderPane(Data, { theme }, 'full-height'),
      route: 'data',
    },
    {
      menuItem: {
        content: 'Events',
        icon: 'calendar check',
        key: 'events',
      },
      render: () => renderPane(Events, {}, 'full-height'),
      route: 'events',
    },
    {
      menuItem: {
        content: 'Logs',
        icon: 'file outline',
        key: 'logs',
      },
      render: () => renderPane(Logs),
      route: 'logs',
    },
    {
      menuItem: {
        content: 'Metrics',
        icon: 'chart bar',
        key: 'metrics',
      },
      render: () => renderPane(Metrics, {}, 'full-height'),
      route: 'metrics',
    },
  ];

  const activeIndex = panes.findIndex((pane) => pane.route === tab);

  const onTabChange = (_event, { activeIndex: newActiveIndex }) => {
    navigate(`/system/${panes[newActiveIndex].route}`);
  };

  if (tab === undefined || activeIndex === -1) {
    return <Navigate replace to={`/system/${panes[0].route}`} />;
  }

  return (
    <div className="system">
      <Segment raised>
        <Tab
          activeIndex={activeIndex > -1 ? activeIndex : 0}
          onTabChange={onTabChange}
          panes={panes}
        />
      </Segment>
    </div>
  );
};

export default System;
