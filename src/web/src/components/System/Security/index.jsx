import './Security.css';
import * as securityApi from '../../../lib/security';
import AdversarialSettings from './AdversarialSettings';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  Button,
  Dimmer,
  Header,
  Icon,
  Loader,
  Message,
  Popup,
  Segment,
  Statistic,
  Tab,
} from 'semantic-ui-react';

const Security = () => {
  const dashboardLoadedRef = useRef(false);
  const dashboardSignatureRef = useRef(null);
  const fetchInFlightRef = useRef(false);
  const mountedRef = useRef(false);
  const pollIntervalRef = useRef(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [dashboard, setDashboard] = useState(null);
  const [refreshing, setRefreshing] = useState(false);

  const fetchData = useCallback(async (showRefreshing = false) => {
    if (document.hidden || !mountedRef.current || fetchInFlightRef.current) {
      return;
    }

    fetchInFlightRef.current = true;
    try {
      if (showRefreshing) {
        setRefreshing(true);
      }

      const dashboardData = await securityApi.getDashboard();
      if (!mountedRef.current || document.hidden) return;
      const signature = JSON.stringify(dashboardData ?? null);
      if (dashboardSignatureRef.current !== signature) {
        dashboardSignatureRef.current = signature;
        setDashboard(dashboardData);
      }

      dashboardLoadedRef.current = true;
      setError(null);
    } catch (fetchError) {
      if (!mountedRef.current || document.hidden) return;
      if (!dashboardLoadedRef.current) {
        setError(fetchError.message || 'Failed to load security data');
      }
    } finally {
      fetchInFlightRef.current = false;
      if (mountedRef.current && !document.hidden) {
        setLoading(false);
        if (showRefreshing) {
          setRefreshing(false);
        }
      }
    }
  }, []);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    const stopPolling = () => {
      if (pollIntervalRef.current) {
        window.clearInterval(pollIntervalRef.current);
        pollIntervalRef.current = null;
      }
    };
    const startPolling = () => {
      if (document.hidden || pollIntervalRef.current) return;

      fetchData();
      pollIntervalRef.current = window.setInterval(fetchData, 30_000);
    };
    const handleVisibilityChange = () => {
      if (document.hidden) {
        setRefreshing(false);
        stopPolling();
      } else {
        startPolling();
      }
    };

    startPolling();
    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      stopPolling();
    };
  }, [fetchData]);

  if (loading) {
    return (
      <Segment placeholder>
        <Dimmer
          active
          inverted
        >
          <Loader>Loading Security Status...</Loader>
        </Dimmer>
      </Segment>
    );
  }

  if (error && !dashboard) {
    return (
      <Message negative>
        <Message.Header>Security Module Unavailable</Message.Header>
        <p>{error}</p>
        <p>Security features may not be enabled on this server.</p>
        <Popup
          content="Try loading the security dashboard again after the service is enabled or the connection recovers."
          position="top center"
          trigger={
            <Button
              onClick={() => fetchData(true)}
              size="small"
            >
              Retry
            </Button>
          }
        />
      </Message>
    );
  }

  const stats = dashboard || {};

  // Check if security is enabled but no data is available yet
  const hasAnyData =
    stats.networkGuardStats ||
    stats.reputationStats ||
    stats.violationStats ||
    stats.eventStats ||
    stats.paranoidStats ||
    stats.honeypotStats ||
    stats.fingerprintStats ||
    stats.canaryStats ||
    stats.entropyStats ||
    stats.consensusStats ||
    stats.verificationStats ||
    stats.disclosureStats ||
    stats.temporalStats;

  if (!hasAnyData) {
    return (
      <Message info>
        <Message.Header>
          <Icon name="info circle" />
          Security System Active - No Activity Yet
        </Message.Header>
        <p>
          The security subsystem is running but hasn't collected data yet. This
          is normal for:
        </p>
        <ul>
          <li>
            <strong>Fresh installations</strong> - Security features need time
            to observe network activity
          </li>
          <li>
            <strong>Standalone mode</strong> - Most security features activate
            when mesh networking is enabled
          </li>
          <li>
            <strong>Low traffic</strong> - Peer reputation, violation tracking,
            and behavioral analysis require peer interactions
          </li>
        </ul>
        <p>
          <strong>To activate security features:</strong>
        </p>
        <ol>
          <li>Connect to the Soulseek network (if not already connected)</li>
          <li>
            Enable mesh networking via DHT/Overlay (check footer for
            connectivity)
          </li>
          <li>Wait for peer connections and transfer activity</li>
        </ol>
        <p>
          Security monitoring will begin automatically once peer activity is
          detected. Check the <strong>Mesh</strong> tab to verify connectivity.
        </p>
        <Popup
          content="Request the latest security snapshot now instead of waiting for the next automatic refresh."
          position="top center"
          trigger={
            <Button
              icon="refresh"
              loading={refreshing}
              onClick={() => fetchData(true)}
              primary
              size="small"
            >
              Refresh Status
            </Button>
          }
        />
      </Message>
    );
  }

  const panes = [
    {
      menuItem: {
        content: 'Status',
        icon: 'shield alternate',
        key: 'status',
      },
      render: () => (
        <Tab.Pane>
          <div className="security-dashboard">
            <div className="security-header">
              <Header as="h3">
                <Icon name="shield alternate" />
                <Header.Content>
                  Security Status
                  <Header.Subheader>
                    Real-time security monitoring
                  </Header.Subheader>
                </Header.Content>
              </Header>
              <Popup
                content="Request the latest security snapshot now to check recent network activity."
                position="top center"
                trigger={
                  <Button
                    icon="refresh"
                    loading={refreshing}
                    onClick={() => fetchData(true)}
                    size="tiny"
                    title="Refresh"
                  />
                }
              />
            </div>

            <Statistic.Group
              size="small"
              widths={4}
            >
              <Statistic color="blue">
                <Statistic.Value>
                  {stats.networkGuardStats?.globalConnections ?? 0}
                </Statistic.Value>
                <Statistic.Label>Active Connections</Statistic.Label>
              </Statistic>
              <Statistic color="teal">
                <Statistic.Value>
                  {stats.reputationStats?.totalPeers ?? 0}
                </Statistic.Value>
                <Statistic.Label>Tracked Peers</Statistic.Label>
              </Statistic>
              <Statistic color="orange">
                <Statistic.Value>
                  {stats.violationStats?.trackedIps ?? 0}
                </Statistic.Value>
                <Statistic.Label>Tracked Violators</Statistic.Label>
              </Statistic>
              <Statistic color="green">
                <Statistic.Value>
                  {stats.eventStats?.totalEvents ?? 0}
                </Statistic.Value>
                <Statistic.Label>Security Events</Statistic.Label>
              </Statistic>
            </Statistic.Group>

            <Segment>
              <Header as="h4">
                <Icon name="info circle" />
                Security Overview
              </Header>
              <p>
                <strong>Network Guard:</strong> Rate limiting and connection
                caps are {stats.networkGuardStats ? 'active' : 'inactive'}.
              </p>
              <p>
                <strong>Peer Reputation:</strong>{' '}
                {stats.reputationStats?.trustedPeers ?? 0} trusted,{' '}
                {stats.reputationStats?.untrustedPeers ?? 0} untrusted peers.
              </p>
              <p>
                <strong>Violations:</strong>{' '}
                {stats.violationStats?.trackedIps ?? 0} IPs,{' '}
                {stats.violationStats?.trackedUsernames ?? 0} usernames tracked.
              </p>
              <p>
                <strong>Crypto Health:</strong> Entropy checks:{' '}
                {stats.entropyStats?.checkCount ?? 0}, Warnings:{' '}
                {stats.entropyStats?.warningCount ?? 0}
              </p>
            </Segment>
          </div>
        </Tab.Pane>
      ),
    },
    {
      menuItem: {
        content: 'Adversarial',
        icon: 'user secret',
        key: 'adversarial',
      },
      render: () => (
        <Tab.Pane>
          <AdversarialSettings />
        </Tab.Pane>
      ),
    },
  ];

  return (
    <Tab
      activeIndex={activeIndex}
      onTabChange={(_event, { activeIndex: nextIndex }) =>
        setActiveIndex(nextIndex)
      }
      panes={panes}
    />
  );
};

export default Security;
