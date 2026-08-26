import './System.css';
import { Switch } from '../Shared';
import React, { Suspense, lazy, useEffect, useRef } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { Icon, Label, Menu, Message, Segment, Tab } from 'semantic-ui-react';

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

// Six named groups instead of one 22-item flat tab strip. Order here is the
// order sections appear in; a pane's `section` field below assigns it to one.
const SECTIONS = [
  { icon: 'home', key: 'overview', title: 'Overview' },
  { icon: 'share alternate', key: 'network', title: 'Network & Mesh' },
  { icon: 'shield alternate', key: 'security', title: 'Security & Trust' },
  { icon: 'magic', key: 'automation', title: 'Automation & Jobs' },
  { icon: 'heartbeat', key: 'diagnostics', title: 'Diagnostics' },
  { icon: 'options', key: 'advanced', title: 'Advanced' },
];

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

const labeledMenuItem = ({ icon, key, label, labelColor = 'grey', text }) => (
  <Menu.Item key={key}>
    <Icon name={icon} />
    {text}
    {label && (
      <Label
        className="system-tab-label"
        color={labelColor}
        size="mini"
      >
        {label}
      </Label>
    )}
  </Menu.Item>
);

const System = ({ options = {}, state = {}, theme }) => {
  const navigate = useNavigate();
  const { tab } = useParams();
  const systemRef = useRef(null);

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
      section: 'overview',
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
      section: 'overview',
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
      section: 'overview',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'share alternate',
        key: 'mesh',
        label: 'Experimental',
        labelColor: 'violet',
        text: 'Mesh',
      }),
      render: () => renderPane(Mesh),
      route: 'mesh',
      section: 'network',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'exchange',
        key: 'bridge',
        label: 'Experimental',
        labelColor: 'violet',
        text: 'Bridge',
      }),
      render: () => renderPane(Bridge),
      route: 'bridge',
      section: 'network',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'music',
        key: 'mediacore',
        label: 'Experimental',
        labelColor: 'violet',
        text: 'MediaCore',
      }),
      render: () => renderPane(MediaCore),
      route: 'mediacore',
      section: 'network',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'random',
        key: 'source-providers',
        label: 'Experimental',
        labelColor: 'violet',
        text: 'Source Providers',
      }),
      render: () => renderPane(SourceProviders, {}, 'full-height'),
      route: 'source-providers',
      section: 'network',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'chart line',
        key: 'swarm-analytics',
        label: 'Experimental',
        labelColor: 'violet',
        text: 'Swarm Analytics',
      }),
      render: () => renderPane(SwarmAnalytics, {}, 'full-height'),
      route: 'swarm-analytics',
      section: 'network',
    },
    {
      menuItem: {
        content: 'Security',
        icon: 'shield alternate',
        key: 'security',
      },
      render: () => renderPane(Security),
      route: 'security',
      section: 'security',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'sliders horizontal',
        key: 'policies',
        label: 'Admin',
        text: 'Policies',
      }),
      render: () => renderPane(AdminPolicies, { options }, 'full-height'),
      route: 'policies',
      section: 'security',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'shield',
        key: 'quarantine-jury',
        label: 'Admin',
        text: 'Quarantine Jury',
      }),
      render: () => renderPane(QuarantineJury, {}, 'full-height'),
      route: 'quarantine-jury',
      section: 'security',
    },
    {
      menuItem: {
        content: 'Jobs',
        icon: 'tasks',
        key: 'jobs',
      },
      render: () => renderPane(Jobs, {}, 'full-height'),
      route: 'jobs',
      section: 'automation',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'magic',
        key: 'automations',
        label: 'Admin',
        text: 'Automations',
      }),
      render: () => renderPane(AutomationCenter, {}, 'full-height'),
      route: 'automations',
      section: 'automation',
    },
    {
      menuItem: {
        content: 'Events',
        icon: 'calendar check',
        key: 'events',
      },
      render: () => renderPane(Events, {}, 'full-height'),
      route: 'events',
      section: 'automation',
    },
    {
      menuItem: {
        content: 'Library Health',
        icon: 'heartbeat',
        key: 'library-health',
      },
      render: () => renderPane(LibraryHealth, {}, 'full-height'),
      route: 'library-health',
      section: 'diagnostics',
    },
    {
      menuItem: {
        content: 'Files',
        icon: 'folder open',
        key: 'files',
      },
      render: () => renderPane(Files, { options, theme }, 'full-height'),
      route: 'files',
      section: 'diagnostics',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'database',
        key: 'data',
        label: 'Admin',
        text: 'Data',
      }),
      render: () => renderPane(Data, { theme }, 'full-height'),
      route: 'data',
      section: 'diagnostics',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'file outline',
        key: 'logs',
        label: 'Admin',
        text: 'Logs',
      }),
      render: () => renderPane(Logs),
      route: 'logs',
      section: 'diagnostics',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'chart bar',
        key: 'metrics',
        label: 'Admin',
        text: 'Metrics',
      }),
      render: () => renderPane(Metrics, {}, 'full-height'),
      route: 'metrics',
      section: 'diagnostics',
    },
    {
      menuItem: {
        content: 'Experience',
        icon: 'compass',
        key: 'experience',
      },
      render: () => renderPane(ExperienceSettings, {}, 'full-height'),
      route: 'experience',
      section: 'advanced',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'plug',
        key: 'integrations',
        label: 'Admin',
        text: 'Integrations',
      }),
      render: () => renderPane(Integrations, { options, state }, 'full-height'),
      route: 'integrations',
      section: 'advanced',
    },
    {
      menuItem: labeledMenuItem({
        icon: 'options',
        key: 'options',
        label: 'Admin',
        text: 'Options',
      }),
      render: () => renderPane(Options, { options, theme }, 'full-height'),
      route: 'options',
      section: 'advanced',
    },
  ];

  const activeIndex = panes.findIndex((pane) => pane.route === tab);

  useEffect(() => {
    const activeItem = systemRef.current?.querySelector(
      '.ui.tabular.menu .active.item',
    );
    activeItem?.scrollIntoView?.({ block: 'nearest', inline: 'nearest' });
  }, [tab]);

  if (tab === undefined || activeIndex === -1) {
    return <Navigate replace to={`/system/${panes[0].route}`} />;
  }

  const activePane = panes[activeIndex];
  const sectionPanes = panes.filter(
    (pane) => pane.section === activePane.section,
  );
  const sectionActiveIndex = sectionPanes.findIndex(
    (pane) => pane.route === activePane.route,
  );

  const onSectionTabChange = (_event, { activeIndex: newIndex }) => {
    navigate(`/system/${sectionPanes[newIndex].route}`);
  };

  const onSectionSelect = (sectionKey) => {
    const firstRoute = panes.find((pane) => pane.section === sectionKey)?.route;
    if (firstRoute) {
      navigate(`/system/${firstRoute}`);
    }
  };

  return (
    <div className="system" ref={systemRef}>
      <Menu
        className="system-section-menu"
        pointing
        secondary
      >
        {SECTIONS.map((section) => (
          <Menu.Item
            active={activePane.section === section.key}
            key={section.key}
            onClick={() => onSectionSelect(section.key)}
          >
            <Icon name={section.icon} />
            {section.title}
          </Menu.Item>
        ))}
      </Menu>
      <Segment raised>
        <Tab
          activeIndex={sectionActiveIndex > -1 ? sectionActiveIndex : 0}
          onTabChange={onSectionTabChange}
          panes={sectionPanes}
        />
      </Segment>
    </div>
  );
};

export default System;
