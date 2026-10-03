import React from 'react';
import { Header, Icon, Label } from 'semantic-ui-react';

const getConnectionStatus = (server) => {
  if (!server) return { color: 'grey', label: 'Waiting for status' };
  if (server.isConnected || server.IsConnected) {
    return { color: 'green', label: 'Connected' };
  }
  if (
    server.isConnecting || server.IsConnecting ||
    server.isLoggingIn || server.IsLoggingIn
  ) {
    return { color: 'yellow', label: 'Connecting' };
  }

  return { color: 'red', label: 'Disconnected' };
};

const SystemOverview = ({ state = {} }) => {
  const connection = getConnectionStatus(state.server);
  const version = state.version?.current || state.version?.full || 'Unavailable';
  const pendingActions = [
    state.pendingRestart && 'Restart required',
    state.pendingReconnect && 'Reconnect required',
    state.shares?.scanPending && 'Share scan pending',
  ].filter(Boolean);

  return (
    <section
      aria-labelledby="system-overview-heading"
      className="system-overview"
      data-testid="system-overview"
    >
      <Header as="h2" id="system-overview-heading">
        System overview
      </Header>
      <div className="system-overview-grid">
        <article className="system-overview-card">
          <span className="system-overview-label">Soulseek connection</span>
          <strong className="system-overview-value">
            <Icon
              aria-hidden="true"
              color={connection.color}
              name="circle"
              size="tiny"
            />{' '}
            {connection.label}
          </strong>
          <span className="system-overview-detail">
            {state.user?.username || 'Account status unavailable'}
          </span>
        </article>
        <article className="system-overview-card">
          <span className="system-overview-label">slskdN version</span>
          <strong className="system-overview-value">{version}</strong>
          {state.version?.isUpdateAvailable === true && (
            <span className="system-overview-detail">
              Version {state.version.latest} is available
            </span>
          )}
        </article>
        <article className="system-overview-card">
          <span className="system-overview-label">Pending actions</span>
          {pendingActions.length > 0 ? (
            <ul className="system-overview-pending">
              {pendingActions.map((action) => <li key={action}>{action}</li>)}
            </ul>
          ) : (
            <strong className="system-overview-value">
              <Label color="green" size="mini">None</Label>
            </strong>
          )}
        </article>
      </div>
    </section>
  );
};

export { getConnectionStatus };
export default SystemOverview;
