import { urlBase } from '../../config';
import * as pods from '../../lib/pods';
import * as portForwarding from '../../lib/portForwarding';
import React, { Component } from 'react';
import {
  Button,
  Card,
  Dimmer,
  Dropdown,
  Form,
  Icon,
  Input,
  Label,
  List,
  Loader,
  Message,
  Modal,
  Popup,
  Segment,
  Statistic,
  Tab,
  Table,
} from 'semantic-ui-react';

const initialState = {
  activeTab: 0,
  availablePortCount: 0,
  availablePorts: [],
  availablePortsLoaded: false,
  createForm: {
    destinationHost: '',
    destinationPort: '',
    localPort: '',
    serviceName: '',
  },
  creatingForwarding: false,
  error: null,
  forwardingStatus: [],
  loading: false,
  pods: [],
  selectedPodDetail: null,
  selectedPodId: null,
  showCreateModal: false,
  stoppingForwarding: false,
  success: null,
  vpnPodMembers: {},
  vpnPodMembersLoaded: false,
};

const AVAILABLE_PORT_PREVIEW_LIMIT = 100;
const STATUS_POLL_INTERVAL_MS = 10_000;
const asArray = (value) => (Array.isArray(value) ? value : []);
const isObject = (value) => value && typeof value === 'object' && !Array.isArray(value);
const forwardingStatusSignature = (statuses) =>
  statuses
    .map((status) =>
      [
        status.activeConnections,
        status.bytesForwarded,
        status.destinationHost,
        status.destinationPort,
        status.isActive,
        status.localPort,
        status.performance?.averageBytesPerConnection,
        status.performance?.isHighThroughput,
        status.podId,
        status.serviceName,
        status.streamMappingEnabled,
      ].join('\u0001'))
    .sort()
    .join('\u0002');

class PortForwarding extends Component {
  availablePortsRequest = null;
  initialized = false;
  mounted = false;
  statusInterval = null;
  statusRequest = null;
  vpnPodMembersRequest = null;

  constructor(props) {
    super(props);
    this.state = initialState;
  }

  componentDidMount() {
    this.mounted = true;
    document.addEventListener('visibilitychange', this.handleVisibilityChange);
    if (!document.hidden) {
      this.initializeComponent();
      this.startStatusPolling();
    }
  }

  componentWillUnmount() {
    this.mounted = false;
    this.stopStatusPolling();
    document.removeEventListener('visibilitychange', this.handleVisibilityChange);
  }

  startStatusPolling = () => {
    if (document.hidden || this.statusInterval) return;
    this.statusInterval = window.setInterval(
      this.fetchForwardingStatus,
      STATUS_POLL_INTERVAL_MS,
    );
  };

  stopStatusPolling = () => {
    if (!this.statusInterval) return;
    window.clearInterval(this.statusInterval);
    this.statusInterval = null;
  };

  handleVisibilityChange = () => {
    if (document.hidden) {
      this.stopStatusPolling();
      return;
    }

    if (!this.initialized) {
      this.initializeComponent();
    } else {
      this.fetchForwardingStatus();
    }
    this.startStatusPolling();
  };

  initializeComponent = async () => {
    this.initialized = true;
    this.setState({ error: null, loading: true });

    try {
      await Promise.all([
        this.fetchPods(),
        this.fetchForwardingStatus(),
      ]);
    } catch (error) {
      console.error('Failed to initialize port forwarding:', error);
      if (this.mounted) this.setState({ error: error.message });
    } finally {
      if (this.mounted) this.setState({ loading: false });
    }
  };

  fetchPods = async () => {
    try {
      const podsList = await pods.list();
      if (this.mounted) {
        this.setState({ pods: asArray(podsList).filter(isObject) });
      }
    } catch (error) {
      console.error('Failed to fetch pods:', error);
      if (this.mounted) this.setState({ pods: [] });
    }
  };

  fetchAvailablePorts = async () => {
    if (this.availablePortsRequest) return this.availablePortsRequest;
    this.availablePortsRequest = (async () => {
      try {
        const result = await portForwarding.getAvailablePorts(
          1_024,
          65_535,
          AVAILABLE_PORT_PREVIEW_LIMIT,
        );
        if (!this.mounted) return;
        const availablePorts = asArray(result?.availablePorts);
        this.setState({
          availablePortCount: Number.isFinite(result?.availablePortCount)
            ? result.availablePortCount
            : availablePorts.length,
          availablePorts,
          availablePortsLoaded: true,
        });
      } catch (error) {
        console.error('Failed to fetch available ports:', error);
      }
    })().finally(() => {
      this.availablePortsRequest = null;
    });
    return this.availablePortsRequest;
  };

  fetchForwardingStatus = async () => {
    if (this.statusRequest) return this.statusRequest;
    this.statusRequest = (async () => {
      try {
        const status = asArray(await portForwarding.getForwardingStatus())
          .filter(isObject);
        if (!this.mounted) return;
        this.setState((previous) =>
          forwardingStatusSignature(previous.forwardingStatus) ===
          forwardingStatusSignature(status)
            ? null
            : { forwardingStatus: status });
      } catch (error) {
        console.error('Failed to fetch forwarding status:', error);
      }
    })().finally(() => {
      this.statusRequest = null;
    });
    return this.statusRequest;
  };

  fetchVpnPodMembers = async () => {
    if (this.vpnPodMembersRequest) return this.vpnPodMembersRequest;
    const podList = this.state.pods;
    const vpnCapablePods = podList.filter(
      (pod) =>
        pod.capabilities?.includes('PrivateServiceGateway') ||
        pod.privateServicePolicy?.enabled === true,
    );

    this.vpnPodMembersRequest = Promise.all(
      vpnCapablePods.map(async (pod) => {
        try {
          return [pod.podId, asArray(await pods.getMembers(pod.podId)).length];
        } catch (error) {
          console.error(`Failed to fetch members for pod ${pod.podId}:`, error);
          return [pod.podId, null];
        }
      }),
    )
      .then((entries) => {
        if (!this.mounted) return;
        this.setState({
          vpnPodMembers: Object.fromEntries(entries),
          vpnPodMembersLoaded: true,
        });
      })
      .finally(() => {
        this.vpnPodMembersRequest = null;
      });
    return this.vpnPodMembersRequest;
  };

  refreshForwardingStatusAfterMutation = async () => {
    if (this.statusRequest) await this.statusRequest;
    return this.fetchForwardingStatus();
  };

  refreshAvailablePortsAfterMutation = async () => {
    if (!this.state.availablePortsLoaded) return;
    if (this.availablePortsRequest) await this.availablePortsRequest;
    return this.fetchAvailablePorts();
  };

  handleTabChange = (_event, { activeIndex }) => {
    this.setState({ activeTab: activeIndex }, () => {
      if (activeIndex === 1 && !this.state.availablePortsLoaded) {
        this.fetchAvailablePorts();
      } else if (activeIndex === 3 && !this.state.vpnPodMembersLoaded) {
        this.fetchVpnPodMembers();
      }
    });
  };

  handlePodSelection = async (podId) => {
    this.setState({
      loading: true,
      selectedPodDetail: null,
      selectedPodId: podId,
    });

    try {
      const podDetail = await pods.get(podId);
      this.setState({ selectedPodDetail: podDetail });
    } catch (error) {
      console.error('Failed to fetch pod detail:', error);
      this.setState({ error: `Failed to load pod details: ${error.message}` });
    } finally {
      this.setState({ loading: false });
    }
  };

  handleCreateForwarding = async () => {
    const { createForm, selectedPodId } = this.state;

    if (!selectedPodId) {
      this.setState({ error: 'Please select a pod first' });
      return;
    }

    // Validate form
    if (
      !createForm.localPort ||
      !createForm.destinationHost ||
      !createForm.destinationPort
    ) {
      this.setState({ error: 'Please fill in all required fields' });
      return;
    }

    const localPort = Number.parseInt(createForm.localPort);
    const destinationPort = Number.parseInt(createForm.destinationPort);

    if (isNaN(localPort) || localPort < 1_024 || localPort > 65_535) {
      this.setState({ error: 'Local port must be between 1024 and 65535' });
      return;
    }

    if (
      isNaN(destinationPort) ||
      destinationPort < 1 ||
      destinationPort > 65_535
    ) {
      this.setState({ error: 'Destination port must be between 1 and 65535' });
      return;
    }

    this.setState({ creatingForwarding: true, error: null });

    try {
      await portForwarding.startForwarding({
        destinationHost: createForm.destinationHost,
        destinationPort,
        localPort,
        podId: selectedPodId,
        serviceName: createForm.serviceName || undefined,
      });

      // Reset form and refresh status
      this.setState({
        createForm: initialState.createForm,
        showCreateModal: false,
      });

      await Promise.all([
        this.refreshAvailablePortsAfterMutation(),
        this.refreshForwardingStatusAfterMutation(),
      ]);
    } catch (error) {
      console.error('Failed to create port forwarding:', error);
      this.setState({ error: error.message });
    } finally {
      this.setState({ creatingForwarding: false });
    }
  };

  handleStopForwarding = async (localPort) => {
    this.setState({ error: null, stoppingForwarding: true, success: null });

    try {
      await portForwarding.stopForwarding(localPort);
      await Promise.all([
        this.refreshAvailablePortsAfterMutation(),
        this.refreshForwardingStatusAfterMutation(),
      ]);
      this.setState({
        success: `Successfully stopped forwarding on port ${localPort}`,
      });
    } catch (error) {
      console.error('Failed to stop port forwarding:', error);
      this.setState({ error: error.message });
    } finally {
      this.setState({ stoppingForwarding: false });
    }
  };

  handleFormChange = (field, value) => {
    this.setState((previousState) => ({
      createForm: {
        ...previousState.createForm,
        [field]: value,
      },
    }));
  };

  render() {
    const {
      availablePortCount,
      availablePorts,
      createForm,
      creatingForwarding,
      error,
      forwardingStatus,
      loading,
      pods,
      selectedPodDetail,
      selectedPodId,
      showCreateModal,
      stoppingForwarding,
      success,
      vpnPodMembers,
    } = this.state;

    // Filter pods that have VPN gateway capability
    const vpnCapablePods = pods.filter(
      (pod) =>
        pod.capabilities?.includes('PrivateServiceGateway') ||
        pod.privateServicePolicy?.enabled === true,
    );
    const vpnPodStatus = Object.fromEntries(
      vpnCapablePods.map((pod) => {
        const rules = forwardingStatus.filter((rule) => rule.podId === pod.podId);
        return [
          pod.podId,
          {
            activeTunnels: rules.filter((rule) => rule.isActive).length,
            members: vpnPodMembers[pod.podId],
            name: pod.name || pod.podId,
            podId: pod.podId,
            status: pod.privateServicePolicy?.enabled ? 'Active' : 'Inactive',
            totalBandwidth: rules.reduce(
              (total, rule) => total + (rule.bytesForwarded || 0),
              0,
            ),
          },
        ];
      }),
    );

    const panes = [
      {
        menuItem: 'Active Forwarding',
        render: () => (
          <Tab.Pane>
            {forwardingStatus.length === 0 ? (
              <Segment placeholder>
                <Icon name="exchange" />
                <h3>No active port forwarding</h3>
                <p>
                  Start forwarding local ports to remote services through VPN
                  tunnels.
                </p>
                <Button
                  disabled={vpnCapablePods.length === 0}
                  onClick={() => this.setState({ showCreateModal: true })}
                  primary
                >
                  Start Forwarding
                </Button>
              </Segment>
            ) : (
              <div>
                <div style={{ marginBottom: '20px', textAlign: 'right' }}>
                  <Button
                    disabled={vpnCapablePods.length === 0}
                    onClick={() => this.setState({ showCreateModal: true })}
                    primary
                  >
                    <Icon name="plus" />
                    Add Forwarding
                  </Button>
                </div>

                <Table celled>
                  <Table.Header>
                    <Table.Row>
                      <Table.HeaderCell>Local Port</Table.HeaderCell>
                      <Table.HeaderCell>Pod</Table.HeaderCell>
                      <Table.HeaderCell>Remote Service</Table.HeaderCell>
                      <Table.HeaderCell>Status</Table.HeaderCell>
                      <Table.HeaderCell>Connections</Table.HeaderCell>
                      <Table.HeaderCell>Data Transferred</Table.HeaderCell>
                      <Table.HeaderCell>Actions</Table.HeaderCell>
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {forwardingStatus.map((forwarding) => (
                      <Table.Row key={forwarding.localPort}>
                        <Table.Cell>
                          <code>localhost:{forwarding.localPort}</code>
                        </Table.Cell>
                        <Table.Cell>
                          {forwarding.podId}
                          {forwarding.serviceName && (
                            <div style={{ color: 'var(--slskd-color-subtle, #666)', fontSize: '0.8em' }}>
                              Service: {forwarding.serviceName}
                            </div>
                          )}
                        </Table.Cell>
                        <Table.Cell>
                          <code>
                            {forwarding.destinationHost}:
                            {forwarding.destinationPort}
                          </code>
                        </Table.Cell>
                        <Table.Cell>
                          <Label color={forwarding.isActive ? 'green' : 'red'}>
                            {forwarding.isActive ? 'Active' : 'Inactive'}
                          </Label>
                        </Table.Cell>
                        <Table.Cell>{forwarding.activeConnections}</Table.Cell>
                        <Table.Cell>
                          {forwarding.bytesForwarded > 0
                            ? `${(forwarding.bytesForwarded / 1_024).toFixed(1)} KB`
                            : '0 KB'}
                        </Table.Cell>
                        <Table.Cell>
                          <Popup
                            content="Stop port forwarding"
                            trigger={
                              <Button
                                color="red"
                                icon="stop"
                                loading={stoppingForwarding}
                                onClick={() =>
                                  this.handleStopForwarding(
                                    forwarding.localPort,
                                  )
                                }
                                size="small"
                              />
                            }
                          />
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table>
              </div>
            )}
          </Tab.Pane>
        ),
      },
      {
        menuItem: 'Available Ports',
        render: () => (
          <Tab.Pane>
            <div style={{ marginBottom: '20px' }}>
              <Statistic.Group size="small">
                <Statistic>
                  <Statistic.Value>{availablePortCount}</Statistic.Value>
                  <Statistic.Label>Available Ports</Statistic.Label>
                </Statistic>
                <Statistic>
                  <Statistic.Value>{forwardingStatus.length}</Statistic.Value>
                  <Statistic.Label>In Use</Statistic.Label>
                </Statistic>
              </Statistic.Group>
            </div>

            <Segment>
              <p>Available ports for forwarding (1024-65535):</p>
              <div
                style={{
                  backgroundColor: 'var(--slskd-color-inset, #f8f9fa)',
                  borderRadius: '4px',
                  fontFamily: 'monospace',
                  fontSize: '12px',
                  maxHeight: '400px',
                  overflowY: 'auto',
                  padding: '10px',
                }}
              >
                {availablePorts.length > 0 ? (
                  availablePorts.join(', ') +
                  (availablePortCount > availablePorts.length
                    ? ` ... (+${availablePortCount - availablePorts.length} more)`
                    : '')
                ) : (
                  <em>No ports available or still loading...</em>
                )}
              </div>
            </Segment>
          </Tab.Pane>
        ),
      },
      {
        menuItem: 'Tunnel Statistics',
        render: () => (
          <Tab.Pane>
            <div style={{ marginBottom: '20px' }}>
              <Statistic.Group widths="four">
                <Statistic>
                  <Statistic.Value>
                    {forwardingStatus.filter((status) => status.isActive).length}
                  </Statistic.Value>
                  <Statistic.Label>Active Tunnels</Statistic.Label>
                </Statistic>
                <Statistic>
                  <Statistic.Value>
                    {forwardingStatus.reduce(
                      (sum, status) => sum + (status.activeConnections || 0),
                      0,
                    )}
                  </Statistic.Value>
                  <Statistic.Label>Total Connections</Statistic.Label>
                </Statistic>
                <Statistic>
                  <Statistic.Value>
                    {(
                      forwardingStatus.reduce(
                        (sum, status) => sum + (status.bytesForwarded || 0),
                        0,
                      ) /
                      1_024 /
                      1_024
                    ).toFixed(2)}{' '}
                    MB
                  </Statistic.Value>
                  <Statistic.Label>Data Transferred</Statistic.Label>
                </Statistic>
                <Statistic>
                  <Statistic.Value>
                    {forwardingStatus.filter(
                      (status) => status.performance?.isHighThroughput,
                    ).length}
                  </Statistic.Value>
                  <Statistic.Label>High Throughput</Statistic.Label>
                </Statistic>
              </Statistic.Group>
            </div>

            <Table celled>
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell>Local Port</Table.HeaderCell>
                  <Table.HeaderCell>Data Transferred</Table.HeaderCell>
                  <Table.HeaderCell>Connections</Table.HeaderCell>
                  <Table.HeaderCell>Average / Connection</Table.HeaderCell>
                  <Table.HeaderCell>Stream Mapping</Table.HeaderCell>
                  <Table.HeaderCell>Throughput</Table.HeaderCell>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {forwardingStatus.map((forwarding) => (
                  <Table.Row key={forwarding.localPort}>
                    <Table.Cell>
                      <code>localhost:{forwarding.localPort}</code>
                    </Table.Cell>
                    <Table.Cell>
                      {`${((forwarding.bytesForwarded || 0) / 1_024).toFixed(1)} KB`}
                    </Table.Cell>
                    <Table.Cell>{forwarding.activeConnections || 0}</Table.Cell>
                    <Table.Cell>
                      {`${((forwarding.performance?.averageBytesPerConnection || 0) / 1_024).toFixed(1)} KB`}
                    </Table.Cell>
                    <Table.Cell>
                      <Label color={forwarding.streamMappingEnabled ? 'green' : 'grey'}>
                        {forwarding.streamMappingEnabled ? 'Enabled' : 'Disabled'}
                      </Label>
                    </Table.Cell>
                    <Table.Cell>
                      {forwarding.performance?.isHighThroughput ? 'High' : 'Normal'}
                    </Table.Cell>
                  </Table.Row>
                ))}
                {forwardingStatus.length === 0 && (
                  <Table.Row>
                    <Table.Cell
                      colSpan={6}
                      textAlign="center"
                    >
                      No active tunnels to display statistics for
                    </Table.Cell>
                  </Table.Row>
                )}
              </Table.Body>
            </Table>
          </Tab.Pane>
        ),
      },
      {
        menuItem: 'VPN Pods',
        render: () => (
          <Tab.Pane>
            <div style={{ marginBottom: '20px' }}>
              <Statistic.Group widths="three">
                <Statistic>
                  <Statistic.Value>
                    {Object.keys(vpnPodStatus).length}
                  </Statistic.Value>
                  <Statistic.Label>VPN-Capable Pods</Statistic.Label>
                </Statistic>
                <Statistic>
                  <Statistic.Value>
                    {Object.values(vpnPodStatus).reduce(
                      (sum, pod) => sum + (pod.members || 0),
                      0,
                    )}
                  </Statistic.Value>
                  <Statistic.Label>Total Members</Statistic.Label>
                </Statistic>
                <Statistic>
                  <Statistic.Value>
                    {Object.values(vpnPodStatus).reduce(
                      (sum, pod) => sum + pod.activeTunnels,
                      0,
                    )}
                  </Statistic.Value>
                  <Statistic.Label>Active Tunnels</Statistic.Label>
                </Statistic>
              </Statistic.Group>
            </div>

            <Table celled>
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell>Pod Name</Table.HeaderCell>
                  <Table.HeaderCell>Members</Table.HeaderCell>
                  <Table.HeaderCell>Active Tunnels</Table.HeaderCell>
                  <Table.HeaderCell>Data Transferred</Table.HeaderCell>
                  <Table.HeaderCell>Status</Table.HeaderCell>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {Object.values(vpnPodStatus).map((pod) => (
                  <Table.Row key={pod.podId}>
                    <Table.Cell>
                      <strong>{pod.name}</strong>
                      <div style={{ color: 'var(--slskd-color-subtle, #666)', fontSize: '0.8em' }}>
                        ID: {pod.podId}
                      </div>
                    </Table.Cell>
                    <Table.Cell>{pod.members ?? 'Unavailable'}</Table.Cell>
                    <Table.Cell>{pod.activeTunnels}</Table.Cell>
                    <Table.Cell>
                      {pod.totalBandwidth > 0
                        ? `${(pod.totalBandwidth / 1_024 / 1_024).toFixed(2)} MB`
                        : '0 MB'}
                    </Table.Cell>
                    <Table.Cell>
                      <Label color={pod.status === 'Active' ? 'green' : 'grey'}>
                        {pod.status}
                      </Label>
                    </Table.Cell>
                  </Table.Row>
                ))}
                {Object.keys(vpnPodStatus).length === 0 && (
                  <Table.Row>
                    <Table.Cell
                      colSpan={5}
                      textAlign="center"
                    >
                      No VPN-capable pods found
                    </Table.Cell>
                  </Table.Row>
                )}
              </Table.Body>
            </Table>
          </Tab.Pane>
        ),
      },
    ];

    return (
      <div style={{ padding: '20px' }}>
        <Dimmer active={loading}>
          <Loader />
        </Dimmer>

        <div style={{ marginBottom: '30px' }}>
          <h2>Port Forwarding</h2>
          <p>
            Forward local ports to remote services through secure VPN tunnels.
          </p>
        </div>

        {error && (
          <Message error>
            <Message.Header>Error</Message.Header>
            <p>{error}</p>
            <Button
              onClick={() => this.setState({ error: null })}
              size="small"
            >
              Dismiss
            </Button>
          </Message>
        )}

        {success && (
          <Message success>
            <Message.Header>Success</Message.Header>
            <p>{success}</p>
            <Button
              onClick={() => this.setState({ success: null })}
              size="small"
            >
              Dismiss
            </Button>
          </Message>
        )}

        {vpnCapablePods.length === 0 && (
          <Message warning>
            <Message.Header>No VPN-Capable Pods</Message.Header>
            <p>
              You need at least one pod with VPN gateway capability to use port
              forwarding.
            </p>
            <p>
              Create or join a pod that has the{' '}
              <code>PrivateServiceGateway</code> capability enabled.
            </p>
          </Message>
        )}

        <Tab
          activeIndex={this.state.activeTab}
          menu={{ pointing: true }}
          onTabChange={this.handleTabChange}
          panes={panes}
        />

        {/* Create Forwarding Modal */}
        <Modal
          onClose={() => this.setState({ showCreateModal: false })}
          open={showCreateModal}
          size="small"
        >
          <Modal.Header>Start Port Forwarding</Modal.Header>
          <Modal.Content>
            <Form>
              <Form.Field>
                <label>VPN Pod</label>
                <Dropdown
                  fluid
                  onChange={(e, { value }) => this.handlePodSelection(value)}
                  options={vpnCapablePods.map((pod) => ({
                    key: pod.podId,
                    text: pod.name || pod.podId,
                    value: pod.podId,
                  }))}
                  placeholder="Select a VPN-capable pod"
                  selection
                  value={selectedPodId || ''}
                />
                {selectedPodDetail && (
                  <div
                    style={{
                      color: 'var(--slskd-color-subtle, #666)',
                      fontSize: '0.9em',
                      marginTop: '10px',
                    }}
                  >
                    <p>
                      <strong>Members:</strong>{' '}
                      {selectedPodDetail.members?.length || 0}
                    </p>
                    {selectedPodDetail.privateServicePolicy?.enabled && (
                      <p>
                        <strong>VPN Gateway:</strong> Enabled
                      </p>
                    )}
                  </div>
                )}
              </Form.Field>

              <Form.Field required>
                <label>Local Port</label>
                <Input
                  max="65535"
                  min="1024"
                  onChange={(e) =>
                    this.handleFormChange('localPort', e.target.value)
                  }
                  placeholder="e.g., 8080"
                  type="number"
                  value={createForm.localPort}
                />
                <small style={{ color: 'var(--slskd-color-subtle, #666)' }}>
                  Port on your local machine (1024-65535)
                </small>
              </Form.Field>

              <Form.Field required>
                <label>Remote Host</label>
                <Input
                  onChange={(e) =>
                    this.handleFormChange('destinationHost', e.target.value)
                  }
                  placeholder="e.g., database.internal.company.com"
                  value={createForm.destinationHost}
                />
                <small style={{ color: 'var(--slskd-color-subtle, #666)' }}>
                  Hostname or IP address of the remote service
                </small>
              </Form.Field>

              <Form.Field required>
                <label>Remote Port</label>
                <Input
                  max="65535"
                  min="1"
                  onChange={(e) =>
                    this.handleFormChange('destinationPort', e.target.value)
                  }
                  placeholder="e.g., 5432"
                  type="number"
                  value={createForm.destinationPort}
                />
                <small style={{ color: 'var(--slskd-color-subtle, #666)' }}>
                  Port number of the remote service
                </small>
              </Form.Field>

              <Form.Field>
                <label>Service Name (Optional)</label>
                <Input
                  onChange={(e) =>
                    this.handleFormChange('serviceName', e.target.value)
                  }
                  placeholder="e.g., postgres-db"
                  value={createForm.serviceName}
                />
                <small style={{ color: 'var(--slskd-color-subtle, #666)' }}>
                  Named service registered in the pod (for better organization)
                </small>
              </Form.Field>
            </Form>
          </Modal.Content>
          <Modal.Actions>
            <Button onClick={() => this.setState({ showCreateModal: false })}>
              Cancel
            </Button>
            <Button
              disabled={
                !selectedPodId ||
                !createForm.localPort ||
                !createForm.destinationHost ||
                !createForm.destinationPort
              }
              loading={creatingForwarding}
              onClick={this.handleCreateForwarding}
              primary
            >
              Start Forwarding
            </Button>
          </Modal.Actions>
        </Modal>
      </div>
    );
  }
}

export default PortForwarding;
