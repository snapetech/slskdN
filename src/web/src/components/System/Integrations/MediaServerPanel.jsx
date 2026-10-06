// <copyright file="MediaServerPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import React, { useState } from 'react';
import {
  buildMediaServerExecutionContract,
  buildMediaServerPathDiagnostic,
  buildMediaServerSyncPreview,
  defaultMediaServerAutomations,
  formatMediaServerExecutionContractReport,
  formatMediaServerSyncReport,
  mediaServerAutomationContracts,
  mediaServerAdapters,
} from '../../../lib/mediaServerIntegrations';
import TooltipButton from '../../Shared/TooltipButton';
import {
  Button,
  Card,
  Form,
  Header,
  Icon,
  Message,
  Popup,
  Segment,
} from 'semantic-ui-react';

const MediaServerPanel = () => {
  const [activeAdapterId, setActiveAdapterId] = useState(
    mediaServerAdapters[0]?.id ?? null,
  );
  const [baseUrl, setBaseUrl] = useState('');
  const [tokenConfigured, setTokenConfigured] = useState(false);
  const [localPath, setLocalPath] = useState('');
  const [serverPath, setServerPath] = useState('');
  const [remotePathFrom, setRemotePathFrom] = useState('');
  const [remotePathTo, setRemotePathTo] = useState('');
  const [userMappingConfigured, setUserMappingConfigured] = useState(false);
  const [confirmationRequired, setConfirmationRequired] = useState(true);
  const [rateLimitPerMinute, setRateLimitPerMinute] = useState(6);
  const [dedupeWindowHours, setDedupeWindowHours] = useState(24);
  const [enabledAutomations, setEnabledAutomations] = useState(
    () => ({ ...defaultMediaServerAutomations }),
  );
  const [syncReport, setSyncReport] = useState(null);
  const [pathDiagnostic, setPathDiagnostic] = useState(null);
  const [contractReport, setContractReport] = useState(null);

  const hasAdapters = mediaServerAdapters.length > 0;
  const hasContracts = mediaServerAutomationContracts.length > 0;

  const getSyncPreview = () =>
    buildMediaServerSyncPreview({
      adapterId: activeAdapterId,
      baseUrl,
      localPath,
      remotePathFrom,
      remotePathTo,
      serverPath,
      tokenConfigured,
    });

  const handlePreviewSync = () => {
    setSyncReport(formatMediaServerSyncReport(getSyncPreview()));
  };

  const handlePathDiagnostic = () => {
    setPathDiagnostic(
      buildMediaServerPathDiagnostic({
        localPath,
        remotePathFrom,
        remotePathTo,
        serverPath,
      }),
    );
  };

  const handleReviewContract = () => {
    const contract = buildMediaServerExecutionContract({
      confirmationRequired,
      dedupeWindowHours,
      enabledAutomations,
      rateLimitPerMinute,
      syncPreview: getSyncPreview(),
      userMappingConfigured,
    });
    setContractReport(formatMediaServerExecutionContractReport(contract));
  };

  const updateAutomation = (automationId, enabled) => {
    setEnabledAutomations((current) => ({
      ...current,
      [automationId]: enabled,
    }));
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="server" />
          Media Servers
        </Card.Header>
        <Card.Meta>
          Local readiness reviews for Plex, Jellyfin/Emby, and Navidrome.
          Nothing here saves credentials or contacts a media server.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        <Message info size="small">
          Enter the values you want to review. They remain in this page only;
          preview and contract actions do not run scans or other server work.
        </Message>

        {!hasAdapters && (
          <Message info size="small">
            No registered media server adapters are available.
          </Message>
        )}

        {!hasContracts && hasAdapters && (
          <Message warning size="small">
            No media server automation contracts are available.
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

        <Form
          className="media-server-review-form"
          onSubmit={(event) => event.preventDefault()}
        >
          <section className="media-server-review-section">
            <Header as="h4">Connection</Header>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Media server base URL"
                id="media-server-base-url"
                label="Media server base URL"
                onChange={(_event, { value }) => setBaseUrl(value)}
                placeholder="https://media.example"
                value={baseUrl}
              />
              <Form.Checkbox
                aria-label="API token is configured"
                checked={tokenConfigured}
                id="media-server-token-configured"
                label="API token is configured"
                onChange={(_event, { checked }) => setTokenConfigured(checked)}
              />
            </Form.Group>
          </section>

          <section className="media-server-review-section">
            <Header as="h4">Path mapping</Header>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Completed-download path on slskdN"
                id="media-server-local-path"
                label="Completed-download path on slskdN"
                onChange={(_event, { value }) => setLocalPath(value)}
                placeholder="/downloads/music"
                value={localPath}
              />
              <Form.Input
                aria-label="Library path on the media server"
                id="media-server-server-path"
                label="Library path on the media server"
                onChange={(_event, { value }) => setServerPath(value)}
                placeholder="/library/music"
                value={serverPath}
              />
            </Form.Group>

            <Form.Group widths="equal">
              <Form.Input
                aria-label="Remote path mapping: from"
                id="media-server-remote-path-from"
                label="Remote path mapping: from"
                onChange={(_event, { value }) => setRemotePathFrom(value)}
                placeholder="/downloads"
                value={remotePathFrom}
              />
              <Form.Input
                aria-label="Remote path mapping: to"
                id="media-server-remote-path-to"
                label="Remote path mapping: to"
                onChange={(_event, { value }) => setRemotePathTo(value)}
                placeholder="/library"
                value={remotePathTo}
              />
            </Form.Group>
          </section>

          <section className="media-server-review-section">
            <Header as="h4">Safety contract</Header>
            <Form.Group widths="equal">
              <Form.Checkbox
                aria-label="Media server user mapping is configured"
                checked={userMappingConfigured}
                id="media-server-user-mapping-configured"
                label="Media server user mapping is configured"
                onChange={(_event, { checked }) =>
                  setUserMappingConfigured(checked)
                }
              />
              <Form.Checkbox
                aria-label="Require confirmation for actions"
                checked={confirmationRequired}
                id="media-server-confirmation-required"
                label="Require confirmation for actions"
                onChange={(_event, { checked }) =>
                  setConfirmationRequired(checked)
                }
              />
            </Form.Group>

            <Form.Group widths="equal">
              <Form.Input
                aria-label="Maximum calls per minute"
                id="media-server-rate-limit"
                label="Maximum calls per minute"
                min={0}
                onChange={(_event, { value }) =>
                  setRateLimitPerMinute(Number(value))
                }
                type="number"
                value={rateLimitPerMinute}
              />
              <Form.Input
                aria-label="Dedupe window in hours"
                id="media-server-dedupe-window"
                label="Dedupe window in hours"
                min={0}
                onChange={(_event, { value }) =>
                  setDedupeWindowHours(Number(value))
                }
                type="number"
                value={dedupeWindowHours}
              />
            </Form.Group>

            <Form.Group grouped>
              <Header as="h5">
                Include automations in the local readiness review
              </Header>
              {mediaServerAutomationContracts.map((automation) => (
                <Form.Checkbox
                  aria-label={`Include ${automation.label} in review`}
                  checked={Boolean(enabledAutomations[automation.id])}
                  id={`media-server-automation-${automation.id}`}
                  key={automation.id}
                  label={`${automation.label}: ${automation.description}`}
                  onChange={(_event, { checked }) =>
                    updateAutomation(automation.id, checked)
                  }
                />
              ))}
            </Form.Group>
          </section>
        </Form>

        <div className="integration-actions">
          <TooltipButton
            disabled={!hasAdapters}
            icon
            labelPosition="left"
            onClick={handlePreviewSync}
            tooltip="Check URL, token, and path readiness. This creates a local report and does not contact the media server."
          >
            <Icon name="sync" />
            Preview Sync
          </TooltipButton>
          <TooltipButton
            disabled={!hasAdapters}
            icon
            labelPosition="left"
            onClick={handlePathDiagnostic}
            tooltip="Compare the local and server paths, applying the optional remote mapping; this check stays in your browser."
          >
            <Icon name="folder open" />
            Path Diagnostic
          </TooltipButton>
          <TooltipButton
            disabled={!hasContracts}
            icon
            labelPosition="left"
            onClick={handleReviewContract}
            primary
            tooltip="Review readiness gates and selected automations. This report runs no media server actions."
          >
            <Icon name="clipboard check" />
            Review Contract
          </TooltipButton>
        </div>

        {contractReport && (
          <Segment style={{ marginTop: '1em' }}>
            <Header as="h4">Contract Review</Header>
            <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {contractReport}
            </pre>
          </Segment>
        )}

        {syncReport && (
          <Segment style={{ marginTop: '1em' }}>
            <Header as="h4">Sync Preview</Header>
            <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {syncReport}
            </pre>
          </Segment>
        )}

        {pathDiagnostic && (
          <Segment style={{ marginTop: '1em' }}>
            <Header as="h4">Path Diagnostic</Header>
            <Message color={pathDiagnostic.color} size="small">
              <Message.Header>{pathDiagnostic.status}</Message.Header>
              <p>{pathDiagnostic.message}</p>
              {pathDiagnostic.mappedPath && (
                <p>
                  Mapped path: <code>{pathDiagnostic.mappedPath}</code>
                </p>
              )}
            </Message>
          </Segment>
        )}
      </Card.Content>
    </Card>
  );
};

export default MediaServerPanel;
