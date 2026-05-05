import * as mesh from '../../../lib/mesh';
import * as soulseekDiscovery from '../../../lib/soulseekDiscovery';
import MeshEvidencePolicy from './MeshEvidencePolicy';
import RealmSubjectIndexConflicts from './RealmSubjectIndexConflicts';
import React, { useEffect, useState } from 'react';
import {
  Button,
  Card,
  Grid,
  Header,
  Icon,
  Label,
  List,
  Loader,
  Message,
  Segment,
  Statistic,
} from 'semantic-ui-react';

const Mesh = () => {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [rendezvousStatus, setRendezvousStatus] = useState(null);
  const [rendezvousUsers, setRendezvousUsers] = useState([]);
  const [capabilityRecords, setCapabilityRecords] = useState([]);
  const [rendezvousLoading, setRendezvousLoading] = useState(false);
  const [rendezvousMessage, setRendezvousMessage] = useState(null);

  const getRendezvousErrorText = (error_, fallback) => {
    const data = error_?.response?.data;
    if (typeof data === 'string') return data;
    if (data?.error) return data.error;
    if (data?.title) return data.title;
    return error_?.message || fallback;
  };

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await mesh.getStats();
        setStats(data);
      } catch (error_) {
        setError(error_.message);
      } finally {
        setLoading(false);
      }
    };

    fetchStats();

    // Refresh stats every 30 seconds
    const interval = setInterval(fetchStats, 30_000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    const fetchRendezvousStatus = async () => {
      try {
        const response = await soulseekDiscovery.getMeshRendezvousStatus();
        setRendezvousStatus(response.data || {});
      } catch (error_) {
        setRendezvousStatus({
          enabled: false,
          error: error_?.response?.data || error_.message,
        });
      }
    };

    fetchRendezvousStatus();
  }, []);

  const handleAddRendezvousInterest = async () => {
    setRendezvousLoading(true);
    setRendezvousMessage(null);
    try {
      await soulseekDiscovery.addMeshRendezvousInterest();
      setRendezvousMessage({
        positive: true,
        text: 'Published the slskdN mesh rendezvous interest on this Soulseek account.',
      });
    } catch (error_) {
      setRendezvousMessage({
        negative: true,
        text:
          error_?.response?.status === 403
            ? 'Soulseek rendezvous is disabled in configuration. Enable mesh.enableSoulseekRendezvous before publishing this public interest.'
            : getRendezvousErrorText(error_, 'Unable to publish rendezvous interest.'),
      });
    } finally {
      setRendezvousLoading(false);
    }
  };

  const handleRemoveRendezvousInterest = async () => {
    setRendezvousLoading(true);
    setRendezvousMessage(null);
    try {
      await soulseekDiscovery.removeMeshRendezvousInterest();
      setRendezvousMessage({
        positive: true,
        text: 'Removed the slskdN mesh rendezvous interest from this Soulseek account.',
      });
    } catch (error_) {
      setRendezvousMessage({
        negative: true,
        text:
          error_?.response?.status === 403
            ? 'Soulseek rendezvous is disabled in configuration. Enable mesh.enableSoulseekRendezvous to manage this public interest from the UI.'
            : getRendezvousErrorText(error_, 'Unable to remove rendezvous interest.'),
      });
    } finally {
      setRendezvousLoading(false);
    }
  };

  const handleLoadRendezvousUsers = async () => {
    setRendezvousLoading(true);
    setRendezvousMessage(null);
    try {
      const response = await soulseekDiscovery.discoverMeshRendezvous();
      const data = response.data || {};
      const users = data.users || [];
      const records = data.capabilityRecords || [];
      setRendezvousUsers(users);
      setCapabilityRecords(records);
      setRendezvousMessage({
        positive: true,
        text: `Discovered ${users.length} Soulseek rendezvous candidate(s) and ${records.length} runtime capability record(s).`,
      });
    } catch (error_) {
      setRendezvousUsers([]);
      setCapabilityRecords([]);
      setRendezvousMessage({
        negative: true,
        text:
          error_?.response?.status === 403
            ? 'Soulseek rendezvous is disabled in configuration. Enable mesh.enableSoulseekRendezvous before querying candidates.'
            : getRendezvousErrorText(error_, 'Unable to load rendezvous users.'),
      });
    } finally {
      setRendezvousLoading(false);
    }
  };

  const getHealthColor = (status) => {
    switch (status) {
      case 'Healthy':
        return 'green';
      case 'Degraded':
        return 'yellow';
      case 'Unhealthy':
        return 'red';
      default:
        return 'grey';
    }
  };

  const getHealthIcon = (status) => {
    switch (status) {
      case 'Healthy':
        return 'checkmark';
      case 'Degraded':
        return 'warning sign';
      case 'Unhealthy':
        return 'remove';
      default:
        return 'question';
    }
  };

  if (loading && !stats) {
    return (
      <Segment>
        <Loader
          active
          inline="centered"
        >
          Loading mesh statistics...
        </Loader>
      </Segment>
    );
  }

  if (error) {
    return (
      <Message error>
        <Message.Header>Failed to load mesh statistics</Message.Header>
        <p>{error}</p>
      </Message>
    );
  }

  return (
    <div>
      <Header as="h2">
        <Icon name="sitemap" />
        Mesh Network Status
      </Header>

      <Grid stackable>
        {/* Overall Health Status */}
        <Grid.Column width={16}>
          <Card fluid>
            <Card.Content>
              <Card.Header>
                <Icon
                  color={getHealthColor(stats?.status)}
                  name={getHealthIcon(stats?.status)}
                />
                Network Health: {stats?.status || 'Unknown'}
              </Card.Header>
              <Card.Description>
                {stats?.description || 'No health information available'}
              </Card.Description>
            </Card.Content>
          </Card>
        </Grid.Column>

        {/* Key Statistics */}
        <Grid.Column width={16}>
          <Segment>
            <Header as="h3">Network Statistics</Header>
            <Statistic.Group size="small">
              <Statistic>
                <Statistic.Value>{stats?.totalPeers || 0}</Statistic.Value>
                <Statistic.Label>Total Peers</Statistic.Label>
              </Statistic>
              <Statistic>
                <Statistic.Value>
                  {stats?.activeDhtSessions || 0}
                </Statistic.Value>
                <Statistic.Label>DHT Sessions</Statistic.Label>
              </Statistic>
              <Statistic>
                <Statistic.Value>
                  {stats?.activeOverlaySessions || 0}
                </Statistic.Value>
                <Statistic.Label>Overlay Sessions</Statistic.Label>
              </Statistic>
              <Statistic>
                <Statistic.Value>
                  {stats?.routingTableSize || 0}
                </Statistic.Value>
                <Statistic.Label>Routing Table Size</Statistic.Label>
              </Statistic>
            </Statistic.Group>
          </Segment>
        </Grid.Column>

        {/* Connection Details */}
        <Grid.Column width={8}>
          <Segment>
            <Header as="h3">
              <Icon name="plug" />
              Connections
            </Header>
            <List
              divided
              relaxed
            >
              <List.Item>
                <List.Content>
                  <List.Header>DHT Nodes</List.Header>
                  <List.Description>
                    {stats?.activeDhtSessions || 0} active connections
                  </List.Description>
                </List.Content>
              </List.Item>
              <List.Item>
                <List.Content>
                  <List.Header>Overlay Sessions</List.Header>
                  <List.Description>
                    {stats?.activeOverlaySessions || 0} active sessions
                  </List.Description>
                </List.Content>
              </List.Item>
              <List.Item>
                <List.Content>
                  <List.Header>Mirrored Sessions</List.Header>
                  <List.Description>
                    {stats?.activeMirroredSessions || 0} relay connections
                  </List.Description>
                </List.Content>
              </List.Item>
              <List.Item>
                <List.Content>
                  <List.Header>Bootstrap Peers</List.Header>
                  <List.Description>
                    {stats?.bootstrapPeers || 0} bootstrap nodes
                  </List.Description>
                </List.Content>
              </List.Item>
            </List>
          </Segment>
        </Grid.Column>

        {/* Performance Metrics */}
        <Grid.Column width={8}>
          <Segment>
            <Header as="h3">
              <Icon name="chart line" />
              Performance
            </Header>
            <List
              divided
              relaxed
            >
              <List.Item>
                <List.Content>
                  <List.Header>DHT Operations/sec</List.Header>
                  <List.Description>
                    {stats?.dhtOperationsPerSecond?.toFixed(1) || '0.0'} ops/sec
                  </List.Description>
                </List.Content>
              </List.Item>
              <List.Item>
                <List.Content>
                  <List.Header>Messages Sent</List.Header>
                  <List.Description>
                    {stats?.messagesSent || 0} total messages
                  </List.Description>
                </List.Content>
              </List.Item>
              <List.Item>
                <List.Content>
                  <List.Header>Messages Received</List.Header>
                  <List.Description>
                    {stats?.messagesReceived || 0} total messages
                  </List.Description>
                </List.Content>
              </List.Item>
              <List.Item>
                <List.Content>
                  <List.Header>Peer Churn Events</List.Header>
                  <List.Description>
                    {stats?.peerChurnEvents || 0} churn events
                  </List.Description>
                </List.Content>
              </List.Item>
            </List>
          </Segment>
        </Grid.Column>

        {/* NAT and Network Info */}
        <Grid.Column width={16}>
          <Segment>
            <Header as="h3">
              <Icon name="shield" />
              Network Configuration
            </Header>
            <Grid>
              <Grid.Column width={8}>
                <List>
                  <List.Item>
                    <List.Content>
                      <List.Header>NAT Type</List.Header>
                      <List.Description>
                        <Label
                          color={
                            stats?.natType === 'Direct' ? 'green' : 'yellow'
                          }
                        >
                          {stats?.natType || 'Unknown'}
                        </Label>
                      </List.Description>
                    </List.Content>
                  </List.Item>
                </List>
              </Grid.Column>
              <Grid.Column width={8}>
                <List>
                  <List.Item>
                    <List.Content>
                      <List.Header>Health Indicators</List.Header>
                      <List.Description>
                        <Label
                          color={stats?.routingTableHealthy ? 'green' : 'red'}
                        >
                          Routing Table:{' '}
                          {stats?.routingTableHealthy ? 'Healthy' : 'Unhealthy'}
                        </Label>
                        <br />
                        <Label
                          color={
                            stats?.peerConnectivityHealthy ? 'green' : 'red'
                          }
                        >
                          Peer Connectivity:{' '}
                          {stats?.peerConnectivityHealthy
                            ? 'Healthy'
                            : 'Unhealthy'}
                        </Label>
                        <br />
                        <Label
                          color={stats?.messageFlowHealthy ? 'green' : 'red'}
                        >
                          Message Flow:{' '}
                          {stats?.messageFlowHealthy ? 'Healthy' : 'Unhealthy'}
                        </Label>
                      </List.Description>
                    </List.Content>
                  </List.Item>
                </List>
              </Grid.Column>
            </Grid>
          </Segment>
        </Grid.Column>

        <Grid.Column width={16}>
          <Segment>
            <Header as="h3">
              <Icon name="users" />
              Soulseek Mesh Rendezvous
            </Header>
            <Message
              icon
              warning={!rendezvousStatus?.enabled}
            >
              <Icon name={rendezvousStatus?.enabled ? 'privacy' : 'lock'} />
              <Message.Content>
                <Message.Header>
                  {rendezvousStatus?.enabled
                    ? 'Opt-in public rendezvous is enabled'
                    : 'Opt-in public rendezvous is disabled'}
                </Message.Header>
                <p>
                  This feature uses the native Soulseek interest graph to find
                  other slskdN mesh-capable accounts. Publishing the interest
                  tag makes this account visibly identifiable as a slskdN mesh
                  participant.
                </p>
                <p>
                  Interest tag:{' '}
                  <code>{rendezvousStatus?.interestTag || 'slskdn-mesh-v1'}</code>
                </p>
                {!rendezvousStatus?.enabled && (
                  <p>
                    Enable <code>mesh.enableSoulseekRendezvous</code> in
                    configuration before using these controls.
                  </p>
                )}
              </Message.Content>
            </Message>
            {rendezvousMessage && (
              <Message
                negative={rendezvousMessage.negative}
                positive={rendezvousMessage.positive}
              >
                {rendezvousMessage.text}
              </Message>
            )}
            <Button.Group>
              <Button
                disabled={!rendezvousStatus?.enabled || rendezvousLoading}
                loading={rendezvousLoading}
                onClick={handleAddRendezvousInterest}
                positive
              >
                <Icon name="bullhorn" />
                Publish Interest
              </Button>
              <Button
                disabled={!rendezvousStatus?.enabled || rendezvousLoading}
                loading={rendezvousLoading}
                onClick={handleRemoveRendezvousInterest}
              >
                <Icon name="remove circle" />
                Remove Interest
              </Button>
              <Button
                disabled={!rendezvousStatus?.enabled || rendezvousLoading}
                loading={rendezvousLoading}
                onClick={handleLoadRendezvousUsers}
                primary
              >
                <Icon name="search" />
                Load Candidates
              </Button>
            </Button.Group>
            {rendezvousUsers.length > 0 && (
              <List
                divided
                relaxed
                style={{ marginTop: '1rem' }}
              >
                {rendezvousUsers.map((user) => (
                  <List.Item key={user.username || user.Username}>
                    <List.Icon
                      name="user"
                      verticalAlign="middle"
                    />
                    <List.Content>
                      <List.Header>{user.username || user.Username}</List.Header>
                      <List.Description>
                        Similarity rating:{' '}
                        {user.rating ?? user.Rating ?? 'not reported'}
                      </List.Description>
                    </List.Content>
                  </List.Item>
                ))}
              </List>
            )}
            {capabilityRecords.length > 0 && (
              <List
                divided
                relaxed
                style={{ marginTop: '1rem' }}
              >
                {capabilityRecords.map((record) => (
                  <List.Item key={`${record.username}-${record.nonce}`}>
                    <List.Icon
                      name={record.signed ? 'certificate' : 'id card outline'}
                      verticalAlign="middle"
                    />
                    <List.Content>
                      <List.Header>{record.username}</List.Header>
                      <List.Description>
                        {record.peerId || 'unsigned peer'}, {' '}
                        {(record.features || []).join(', ') || 'no features'}, {' '}
                        overlay port {record.overlayPort || 'not advertised'}
                      </List.Description>
                    </List.Content>
                  </List.Item>
                ))}
              </List>
            )}
          </Segment>
        </Grid.Column>

        <Grid.Column width={16}>
          <MeshEvidencePolicy />
        </Grid.Column>

        <Grid.Column width={16}>
          <RealmSubjectIndexConflicts />
        </Grid.Column>
      </Grid>
    </div>
  );
};

export default Mesh;
