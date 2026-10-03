import '../System.css';
import { createLogsHubConnection } from '../../../lib/hubFactory';
import React, { useEffect, useMemo, useState } from 'react';
import {
  Button,
  ButtonGroup,
  Header,
  Message,
  Popup,
  Table,
} from 'semantic-ui-react';

const maxLogs = 500;

const filters = [
  {
    accessibleName: 'Show all severities',
    description: 'Show every severity so you can inspect the full live log feed.',
    label: 'All',
    level: 'all',
  },
  {
    accessibleName: 'Show information logs',
    color: 'blue',
    description: 'Show informational records to follow normal application activity.',
    label: 'Info',
    level: 'Information',
  },
  {
    accessibleName: 'Show warning logs',
    color: 'yellow',
    description: 'Show warnings to review conditions that may need attention.',
    label: 'Warn',
    level: 'Warning',
  },
  {
    accessibleName: 'Show error logs',
    color: 'red',
    description: 'Show errors to focus on failed operations and their causes.',
    label: 'Error',
    level: 'Error',
  },
  {
    accessibleName: 'Show debug logs',
    description: 'Show debug records when investigating detailed application behavior.',
    label: 'Debug',
    level: 'Debug',
  },
];

const levels = {
  Debug: 'DBG',
  Error: 'ERR',
  Information: 'INF',
  Warning: 'WRN',
};

const getErrorMessage = (error, fallback) =>
  error?.message || fallback;

const formatTimestamp = (timestamp) => {
  const date = new Date(timestamp);
  return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}:${date.getSeconds().toString().padStart(2, '0')}`;
};

const Logs = () => {
  const [logs, setLogs] = useState([]);
  const [filterLevel, setFilterLevel] = useState('all');
  const [connectionStatus, setConnectionStatus] = useState('connecting');
  const [connectionError, setConnectionError] = useState('');
  const [connectionAttempt, setConnectionAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    const logsHub = createLogsHubConnection();

    logsHub.on('buffer', (buffer) => {
      if (!active) return;

      setLogs(Array.isArray(buffer) ? [...buffer].reverse().slice(0, maxLogs) : []);
      setConnectionStatus('connected');
      setConnectionError('');
    });

    logsHub.on('log', (log) => {
      if (!active) return;

      setLogs((current) => [log, ...current].slice(0, maxLogs));
      setConnectionStatus('connected');
      setConnectionError('');
    });

    logsHub.onreconnecting((error) => {
      if (!active) return;

      setConnectionStatus('reconnecting');
      setConnectionError(getErrorMessage(error, 'The live log connection was interrupted.'));
    });

    logsHub.onreconnected(() => {
      if (!active) return;

      setConnectionStatus('connected');
      setConnectionError('');
    });

    logsHub.onclose((error) => {
      if (!active) return;

      setConnectionStatus('disconnected');
      setConnectionError(getErrorMessage(error, 'The live log connection closed.'));
    });

    logsHub.start().then(
      () => {
        if (active) {
          setConnectionStatus('connected');
          setConnectionError('');
        }
      },
      (error) => {
        if (active) {
          setConnectionStatus('disconnected');
          setConnectionError(getErrorMessage(error, 'Unable to connect to live logs.'));
        }
      },
    );

    return () => {
      active = false;
      logsHub.stop().catch((error) => {
        console.error('[Logs] Failed to stop hub connection:', error);
      });
    };
  }, [connectionAttempt]);

  const filteredLogs = useMemo(
    () =>
      filterLevel === 'all'
        ? logs
        : logs.filter((log) => log.level === filterLevel),
    [filterLevel, logs],
  );

  const retryConnection = () => {
    setConnectionError('');
    setConnectionStatus('connecting');
    setConnectionAttempt((attempt) => attempt + 1);
  };

  const connectionMessage = {
    connected: null,
    connecting: {
      content: 'Recent log entries will appear here as they arrive.',
      header: 'Connecting to live logs',
      info: true,
    },
    disconnected: {
      content: connectionError,
      header: 'Live logs are disconnected',
      warning: true,
    },
    reconnecting: {
      content: connectionError
        ? `${connectionError} Buffered entries remain available while the connection recovers.`
        : 'Buffered entries remain available while the connection recovers.',
      header: 'Reconnecting to live logs',
      warning: true,
    },
  }[connectionStatus];

  return (
    <div className="logs">
      <Header as="h2">
        <Header.Content>System Logs</Header.Content>
        <Header.Subheader>
          Monitor the most recent application records as they arrive.
        </Header.Subheader>
      </Header>

      <div className="logs-controls">
        <ButtonGroup
          aria-label="Filter log entries by severity"
          className="logs-filter-buttons"
          role="group"
        >
          {filters.map(({ accessibleName, color, description, label, level }) => {
            const active = filterLevel === level;
            return (
              <Popup
                key={level}
                content={description}
                on={['hover', 'focus']}
                position="top center"
                trigger={(
                  <Button
                    active={active}
                    aria-label={accessibleName}
                    aria-pressed={active}
                    color={active ? color : undefined}
                    onClick={() => setFilterLevel(level)}
                    toggle
                  >
                    {label}
                  </Button>
                )}
              />
            );
          })}
        </ButtonGroup>

        <span
          aria-live="polite"
          className="logs-count"
        >
          {connectionStatus === 'connected'
            ? `Showing ${filteredLogs.length} of ${logs.length} logs`
            : `Showing ${filteredLogs.length} of ${logs.length} buffered logs`}
        </span>
      </div>

      {connectionMessage && (
        <Message
          className="logs-connection-message"
          info={connectionMessage.info}
          warning={connectionMessage.warning}
        >
          <Message.Header>{connectionMessage.header}</Message.Header>
          <p>{connectionMessage.content}</p>
          {connectionStatus === 'disconnected' && (
            <Popup
              content="Retry the live log connection to receive new records."
              on={['hover', 'focus']}
              position="top center"
              trigger={(
                <Button
                  aria-label="Retry live log connection"
                  className="logs-retry"
                  onClick={retryConnection}
                  size="small"
                >
                  Retry
                </Button>
              )}
            />
          )}
        </Message>
      )}

      <div
        aria-label="System log entries"
        className="logs-table-scroll"
        role="region"
        tabIndex={0}
      >
        <Table
          aria-label="System log entries"
          className="logs-table"
          compact="very"
          unstackable
        >
          <Table.Header>
            <Table.Row>
              <Table.HeaderCell>Timestamp</Table.HeaderCell>
              <Table.HeaderCell>Level</Table.HeaderCell>
              <Table.HeaderCell>Message</Table.HeaderCell>
            </Table.Row>
          </Table.Header>
          <Table.Body className="logs-table-body">
            {filteredLogs.length === 0 ? (
              <Table.Row>
                <Table.Cell
                  colSpan="3"
                  textAlign="center"
                >
                  {connectionStatus === 'connected'
                    ? 'No logs match the selected filter'
                    : 'No buffered log entries'}
                </Table.Cell>
              </Table.Row>
            ) : (
              filteredLogs.map((log, index) => (
                <Table.Row
                  key={`${log.timestamp}-${log.level}-${index}`}
                  negative={log.level === 'Error'}
                  warning={log.level === 'Warning'}
                >
                  <Table.Cell>
                    {formatTimestamp(log.timestamp)}
                  </Table.Cell>
                  <Table.Cell>{levels[log.level] || log.level}</Table.Cell>
                  <Table.Cell className="logs-table-message">
                    {log.message}
                  </Table.Cell>
                </Table.Row>
              ))
            )}
          </Table.Body>
        </Table>
      </div>
    </div>
  );
};

export default Logs;
