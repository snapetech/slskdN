// <copyright file="MediaServerPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import React, { useState } from 'react';
import {
  buildMediaServerExecutionContract,
  buildMediaServerPathDiagnostic,
  buildMediaServerSyncPreview,
  formatMediaServerExecutionContractReport,
  formatMediaServerSyncReport,
  mediaServerAutomationContracts,
  mediaServerAdapters,
} from '../../../lib/mediaServerIntegrations';
import {
  Button,
  Card,
  Header,
  Icon,
  Message,
  Popup,
  Segment,
  Table,
} from 'semantic-ui-react';

const MediaServerPanel = () => {
  const [activeAdapterId, setActiveAdapterId] = useState(
    mediaServerAdapters && mediaServerAdapters.length > 0
      ? mediaServerAdapters[0].id
      : null,
  );
  const [executing, setExecuting] = useState(false);
  const [syncReport, setSyncReport] = useState(null);
  const [pathDiagnostic, setPathDiagnostic] = useState(null);
  const [contractReport, setContractReport] = useState(null);
  const [error, setError] = useState(null);

  const hasAdapters = Object.keys(mediaServerAdapters || {}).length > 0;
  const hasContracts =
    Object.keys(mediaServerAutomationContracts || {}).length > 0;

  const handlePreviewSync = async () => {
    setExecuting(true);
    setError(null);
    setSyncReport(null);

    try {
      const report = await buildMediaServerSyncPreview({
        adapterId: activeAdapterId,
      });
      setSyncReport(report);
    } catch (err) {
      setError(
        err?.response?.data ||
          err?.response?.statusText ||
          err?.message ||
          'Failed to preview media server sync.',
      );
    } finally {
      setExecuting(false);
    }
  };

  const handlePathDiagnostic = async () => {
    setExecuting(true);
    setError(null);
    setPathDiagnostic(null);

    try {
      const diagnostic = await buildMediaServerPathDiagnostic({
        adapterId: activeAdapterId,
      });
      setPathDiagnostic(diagnostic);
    } catch (err) {
      setError(
        err?.response?.data ||
          err?.response?.statusText ||
          err?.message ||
          'Failed to run path diagnostic.',
      );
    } finally {
      setExecuting(false);
    }
  };

  const handleExecuteContract = async () => {
    setExecuting(true);
    setError(null);
    setContractReport(null);

    try {
      const report = await buildMediaServerExecutionContract({
        adapterId: activeAdapterId,
      });
      setContractReport(report);
    } catch (err) {
      setError(
        err?.response?.data ||
          err?.response?.statusText ||
          err?.message ||
          'Failed to execute media server contract.',
      );
    } finally {
      setExecuting(false);
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="server" />
          Media Servers
        </Card.Header>
        <Card.Meta>
          Optional Plex, Jellyfin/Emby, and Navidrome integration planning and
          path diagnostics.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        {!hasAdapters && (
          <Message
            info
            size="small"
          >
            No registered media server adapters are available. Configure
            supported adapters in the runtime before enabling automation.
          </Message>
        )}

        {!hasContracts && hasAdapters && (
          <Message
            warning
            size="small"
          >
            Automation contracts are not configured. Media server operations
            will be skipped until at least one contract is defined.
          </Message>
        )}

        {error && (
          <Message
            negative
            size="small"
          >
            <p>{error}</p>
          </Message>
        )}

        {hasAdapters && (
          <div className="integration-actions">
            {mediaServerAdapters.map((adapter) => (
              <Popup
                content={`Review ${adapter.label} integration readiness and path mapping.`}
                key={adapter.id}
                position="top center"
                trigger={
                  <Button
                    aria-label={`Review ${adapter.label} sync readiness`}
                    basic={activeAdapterId !== adapter.id}
                    color={
                      activeAdapterId === adapter.id ? 'purple' : undefined
                    }
                    disabled={executing}
                    icon
                    labelPosition="left"
                    onClick={() => setActiveAdapterId(adapter.id)}
                  >
                    <Icon name={adapter.icon || 'server'} />
                    {adapter.label}
                  </Button>
                }
              />
            ))}
          </div>
        )}

        <div
          className="integration-actions"
          style={{ marginTop: hasAdapters ? '1em' : undefined }}
        >
          <Button
            disabled={!hasAdapters || executing}
            icon
            labelPosition="left"
            loading={executing}
            onClick={handlePreviewSync}
          >
            <Icon name="sync" />
            Preview Sync
          </Button>
          <Button
            disabled={!hasAdapters || executing}
            icon
            labelPosition="left"
            loading={executing}
            onClick={handlePathDiagnostic}
          >
            <Icon name="folder open" />
            Path Diagnostic
          </Button>
          <Button
            disabled={!hasAdapters || executing}
            icon
            labelPosition="left"
            loading={executing}
            onClick={handleExecuteContract}
            primary
          >
            <Icon name="play" />
            Execute Contract
          </Button>
        </div>

        {contractReport && (
          <Segment style={{ marginTop: '1em' }}>
            <Header as="h4">Execution Contract</Header>
            <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {formatMediaServerExecutionContractReport(contractReport)}
            </pre>
          </Segment>
        )}

        {syncReport && (
          <Segment style={{ marginTop: '1em' }}>
            <Header as="h4">Sync Preview</Header>
            <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {formatMediaServerSyncReport(syncReport)}
            </pre>
          </Segment>
        )}

        {pathDiagnostic && (
          <Segment style={{ marginTop: '1em' }}>
            <Header as="h4">Path Diagnostic</Header>
            <Table>
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell>Library</Table.HeaderCell>
                  <Table.HeaderCell>Detected Path</Table.HeaderCell>
                  <Table.HeaderCell>Normalized Path</Table.HeaderCell>
                  <Table.HeaderCell>Status</Table.HeaderCell>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {(pathDiagnostic.paths || []).map((entry) => (
                  <Table.Row key={entry.library}>
                    <Table.Cell>{entry.library}</Table.Cell>
                    <Table.Cell>{entry.detectedPath}</Table.Cell>
                    <Table.Cell>{entry.normalizedPath}</Table.Cell>
                    <Table.Cell>
                      {entry.exists ? 'Accessible' : 'No access'}
                    </Table.Cell>
                  </Table.Row>
                ))}
              </Table.Body>
            </Table>
            {pathDiagnostic.errorSummary && (
              <p style={{ marginTop: '0.5em', color: '#9f3a38' }}>
                {pathDiagnostic.errorSummary}
              </p>
            )}
          </Segment>
        )}
      </Card.Content>
    </Card>
  );
};

export default MediaServerPanel;
